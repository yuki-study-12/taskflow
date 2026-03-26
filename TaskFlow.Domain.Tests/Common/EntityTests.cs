using TaskFlow.Domain.Common;
using Xunit;

namespace TaskFlow.Domain.Tests.Common;

// テスト用ヘルパー
file sealed class TestEntity(Guid id) : Entity<Guid>(id)
{
}

file sealed class OtherEntity(Guid id) : Entity<Guid>(id)
{
}

public class EntityTests
{
    [Fact]
    public void Equals_SameTypeAndSameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_SameTypeAndDifferentId_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_DifferentTypeAndSameId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new OtherEntity(id);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void GetHashCode_EqualEntities_SameHashCode()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
