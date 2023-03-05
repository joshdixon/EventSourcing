using EventSourcing.Contracts;
using EventSourcing.Contracts.Features.Products;
using EventSourcing.Infrastructure;
using EventSourcing.Infrastructure.Domain;
using EventStore.Client;
using Microsoft.Extensions.Options;

namespace EventSourcing.Service.Features.Products;

internal class Handler : RequestHandler<CreateProductRequest>
{
    private readonly IAggregateRepository<Product> _repository;

    public Handler(ILogger<Handler> logger, IAggregateRepository<Product> repository) : base(logger)
    {
        _repository = repository;
    }

    public override async Task<Result> Handle(CreateProductRequest request, CancellationToken cancelToken)
    {
        Product product = Product.Initialize(request.Name, request.Description);
        await _repository.Add(product, cancelToken);
        return Ok();
    }
}