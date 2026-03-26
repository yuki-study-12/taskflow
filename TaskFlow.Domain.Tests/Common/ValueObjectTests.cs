using TaskFlow.Domain.Common;
using Xunit;

namespace TaskFlow.Domain.Tests.Common;

// テスト用ヘルパー
file sealed class Money(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        var a = new Money(100m, "JPY");
        var b = new Money(100m, "JPY");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_DifferentComponents_ReturnsFalse()
    {
        var a = new Money(100m, "JPY");
        var b = new Money(200m, "JPY");

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new Money(100m, "JPY");

        Assert.False(a.Equals(null));
        Assert.True(a != null);
    }

    [Fact]
    public void GetHashCode_EqualObjects_SameHashCode()
    {
        var a = new Money(100m, "JPY");
        var b = new Money(100m, "JPY");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void OperatorNotEqual_DifferentCurrency_ReturnsTrue()
    {
        var a = new Money(100m, "JPY");
        var b = new Money(100m, "USD");

        Assert.True(a != b);
    }
}
