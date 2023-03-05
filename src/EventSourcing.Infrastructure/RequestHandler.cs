using MassTransit;
using Microsoft.Extensions.Logging;
using EventSourcing.Contracts;

namespace EventSourcing.Infrastructure;

public abstract class RequestHandler<TRequest, TResult> : IConsumer<TRequest>
    where TRequest : class, Contracts.IRequest<Result<TResult>>
{
    protected readonly ILogger _logger;

    protected RequestHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TRequest> context)
    {
        Result<TResult> result;

        Type type = GetType();
        using (ServiceActivity activity = new ServiceActivity(_logger, type.FullName ?? type.Name, nameof(Handle),
                   ("request", context.Message)))
        {
            result = await Handle(context.Message, CancellationToken.None);
        }

        await context.RespondAsync(result);
    }

    public abstract Task<Result<TResult>> Handle(TRequest request, CancellationToken cancelToken);

    public Result<TResult> Fail(string errorMessage) => Result<TResult>.Fail(errorMessage, _logger);
    public Result<TResult> NotFound(string errorMessage) => Result<TResult>.NotFound(errorMessage, _logger);
    public Result<TResult> Ok(TResult result) => Result<TResult>.Succeed(result);
}

public abstract class RequestHandler<TRequest> : IConsumer<TRequest>
    where TRequest : class, Contracts.IRequest<Result>
{
    protected readonly ILogger _logger;

    protected RequestHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TRequest> context)
    {
        Result result;

        Type type = GetType();
        using (ServiceActivity activity = new ServiceActivity(_logger, type.FullName ?? type.Name, nameof(Handle),
                   ("request", context.Message)))
        {
            result = await Handle(context.Message, CancellationToken.None);
        }

        await context.RespondAsync(result);
    }

    public abstract Task<Result> Handle(TRequest request, CancellationToken cancelToken);

    public Result Fail(string errorMessage) => Result.Fail(errorMessage, _logger);
    public Result NotFound(string errorMessage) => Result.NotFound(errorMessage, _logger);
    public Result Ok() => Result.Succeed();
}