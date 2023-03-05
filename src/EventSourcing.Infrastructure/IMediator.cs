using EventSourcing.Contracts;

namespace EventSourcing.Infrastructure;

public interface IMediator
{
    public Task<Result> Send(IRequest<Result> request, CancellationToken cancelToken = default);

    public Task<Result<TResult>> Send<TResult>(IRequest<Result<TResult>> request,
        CancellationToken cancelToken = default)
        where TResult : class;
}