using EventSourcing.Infrastructure.Domain;

namespace EventSourcing.Service.Features.Products;

public class Product : Aggregate
{
    public string Name { get; set; }
    public string Description { get; set; }

    private Product(
        Guid id,
        string name,
        string description)
    {
        var @event = new ProductCreated(id, name, description);

        Enqueue(@event);
        Apply(@event);
    }

    public override void When(object @event)
    {
        switch (@event)
        {
            case ProductCreated productCreated:
                Apply(productCreated);
                return;
            case ProductUpdated productUpdated:
                Apply(productUpdated);
                return;
        }
    }

    public static Product Initialize(string name, string description) => new(Guid.NewGuid(), name, description);

    private void Apply(ProductCreated productCreated)
    {
        Id = productCreated.Id;
        Name = productCreated.Name;
        Description = productCreated.Description;
    }

    private void Apply(ProductUpdated productUpdated)
    {
        Name = productUpdated.Name;
        Description = productUpdated.Description;
    }
}

public record ProductCreated(Guid Id, string Name, string Description);

public record ProductUpdated(Guid Id, string Name, string Description);