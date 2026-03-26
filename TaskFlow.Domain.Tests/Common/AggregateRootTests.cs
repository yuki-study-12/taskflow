using TaskFlow.Domain.Common;
using Xunit;

namespace TaskFlow.Domain.Tests.Common;

// テスト用ヘルパー
file sealed class TestDomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

file sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
{
    public void AddEvent(IDomainEvent e) => RaiseDomainEvent(e);
}

public class AggregateRootTests
{
    [Fact]
    public void DomainEvents_NewAggregate_IsEmpty()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_AfterRaise_EventIsPresent()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var evt = new TestDomainEvent();

        aggregate.AddEvent(evt);

        Assert.Single(aggregate.DomainEvents);
        Assert.Same(evt, aggregate.DomainEvents[0]);
    }

    [Fact]
    public void ClearDomainEvents_AfterClear_IsEmpty()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.AddEvent(new TestDomainEvent());

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_MultipleEvents_PreservedInOrder()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var evt1 = new TestDomainEvent();
        var evt2 = new TestDomainEvent();
        var evt3 = new TestDomainEvent();

        aggregate.AddEvent(evt1);
        aggregate.AddEvent(evt2);
        aggregate.AddEvent(evt3);

        Assert.Equal(3, aggregate.DomainEvents.Count);
        Assert.Same(evt1, aggregate.DomainEvents[0]);
        Assert.Same(evt2, aggregate.DomainEvents[1]);
        Assert.Same(evt3, aggregate.DomainEvents[2]);
    }
}
