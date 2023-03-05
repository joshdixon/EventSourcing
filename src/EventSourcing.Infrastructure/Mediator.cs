using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Elastic.Apm.Api;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.DependencyInjection;
using EventSourcing.Contracts;

namespace EventSourcing.Infrastructure;

public class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();

    private readonly MassTransit.Mediator.IMediator _masstransitMediator;
    private readonly IBus _masstransitBus;

    public Mediator(MassTransit.Mediator.IMediator masstransitMediator, IBus masstransitBus)
    {
        _masstransitMediator = masstransitMediator;
        _masstransitBus = masstransitBus;
    }

    public async Task<Result> Send(IRequest<Result> request, CancellationToken cancelToken = default)
    {
        var requestType = request.GetType();

        var handler = (RequestHandlerWrapper)_requestHandlers.GetOrAdd(requestType,
            static t => (RequestHandlerBase)(Activator.CreateInstance(
                                                 typeof(RequestHandlerWrapperImpl<>).MakeGenericType(t))
                                             ?? throw new InvalidOperationException(
                                                 $"Could not create wrapper type for {t}")));

        return await handler.Send(request, _masstransitMediator, _masstransitBus, cancelToken);
    }

    public async Task<Result<TResult>> Send<TResult>(IRequest<Result<TResult>> request,
        CancellationToken cancelToken = default)
        where TResult : class
    {
        var requestType = request.GetType();

        var handler = (RequestHandlerWrapper<TResult>)_requestHandlers.GetOrAdd(requestType,
            static t => (RequestHandlerBase)(Activator.CreateInstance(
                                                 typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(t,
                                                     typeof(TResult)))
                                             ?? throw new InvalidOperationException(
                                                 $"Could not create wrapper type for {t}")));

        return await handler.Send(request, _masstransitMediator, _masstransitBus, cancelToken);
    }
}

internal abstract class RequestHandlerBase
{
    public abstract Task<object?> Send(object request, MassTransit.Mediator.IMediator mediator, IBus bus,
        CancellationToken cancelToken);
}

internal abstract class RequestHandlerWrapper<TResult> : RequestHandlerBase
{
    public abstract Task<Result<TResult>> Send(IRequest<Result<TResult>> request,
        MassTransit.Mediator.IMediator mediator, IBus bus, CancellationToken cancelToken);
}

internal abstract class RequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task<Result> Send(IRequest<Result> request,
        MassTransit.Mediator.IMediator mediator, IBus bus, CancellationToken cancelToken);
}

internal class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
    where TRequest : class, IRequest<Result>
{
    public override async Task<object?> Send(object request, MassTransit.Mediator.IMediator mediator, IBus bus,
        CancellationToken cancelToken)
        => await Send((IRequest<Result>)request, mediator, bus, cancelToken);

    public override async Task<Result> Send(IRequest<Result> request,
        MassTransit.Mediator.IMediator mediator, IBus bus, CancellationToken cancelToken)
    {
        bool isLocal = MessagingConfiguration.ConsumerMessageTypes.Contains(request.GetType());

        var requestClient = isLocal ? mediator.CreateRequestClient<TRequest>() : bus.CreateRequestClient<TRequest>();

        var response = await requestClient.GetResponse<Result>(request, cancelToken);

        return response.Message;
    }
}

internal class RequestHandlerWrapperImpl<TRequest, TResult> : RequestHandlerWrapper<TResult>
    where TRequest : class, IRequest<Result<TResult>>
    where TResult : class
{
    public override async Task<object?> Send(object request, MassTransit.Mediator.IMediator mediator, IBus bus,
        CancellationToken cancelToken)
        => await Send((IRequest<TResult>)request, mediator, bus, cancelToken);

    public override async Task<Result<TResult>> Send(IRequest<Result<TResult>> request,
        MassTransit.Mediator.IMediator mediator, IBus bus, CancellationToken cancelToken)
    {
        bool isLocal = MessagingConfiguration.ConsumerMessageTypes.Contains(request.GetType());

        var requestClient = isLocal ? mediator.CreateRequestClient<TRequest>() : bus.CreateRequestClient<TRequest>();

        var response = await requestClient.GetResponse<Result<TResult>>(request, cancelToken);

        return response.Message;
    }
}