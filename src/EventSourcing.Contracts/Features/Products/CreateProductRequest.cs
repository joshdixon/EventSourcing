namespace EventSourcing.Contracts.Features.Products;

public record CreateProductRequest : IRequest<Result>
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
}