namespace EventSourcing.Infrastructure.Domain;

public interface IProjection
{
    void When(object @event);
}