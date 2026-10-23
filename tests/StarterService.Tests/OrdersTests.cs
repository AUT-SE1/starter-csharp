using StarterService;
using Xunit;

namespace StarterService.Tests;

/// <summary>Unit tests: validation and storage, with no HTTP in sight.</summary>
public class OrdersTests
{
    [Fact]
    public void ValidateCreateOrder_WhenRequestIsValid_ReturnsCleanedValues()
    {
        var (item, quantity) = Orders.ValidateCreateOrder("  notebook  ", 3);

        Assert.Equal("notebook", item);
        Assert.Equal(3, quantity);
    }

    [Theory]
    [InlineData(null, 1, "item")]
    [InlineData("", 1, "item")]
    [InlineData("   ", 1, "item")]
    [InlineData("book", null, "quantity")]
    [InlineData("book", 0, "quantity")]
    [InlineData("book", -5, "quantity")]
    [InlineData("book", 1001, "quantity")]
    public void ValidateCreateOrder_WhenInputIsInvalid_NamesTheOffendingField(
        string? item, int? quantity, string expectedField)
    {
        var failure = Assert.Throws<ValidationException>(
            () => Orders.ValidateCreateOrder(item, quantity));

        Assert.Equal(expectedField, failure.Field);
        Assert.Equal(Orders.ErrValidation, failure.AsBody().Error);
    }

    [Fact]
    public void ValidateCreateOrder_WhenItemIsTooLong_IsRejected()
    {
        var tooLong = new string('a', Orders.MaxItemLength + 1);

        var failure = Assert.Throws<ValidationException>(
            () => Orders.ValidateCreateOrder(tooLong, 1));

        Assert.Equal("item", failure.Field);
    }

    [Theory]
    [InlineData(Orders.MinQuantity)]
    [InlineData(Orders.MaxQuantity)]
    public void ValidateCreateOrder_AtTheQuantityBoundaries_Succeeds(int quantity)
    {
        var (_, accepted) = Orders.ValidateCreateOrder("book", quantity);

        Assert.Equal(quantity, accepted);
    }

    [Fact]
    public void ValidateCreateOrder_WhenItemIsNonAscii_KeepsItIntact()
    {
        // The limit is 64 characters, not 64 bytes — a Persian item name must fit.
        var (item, _) = Orders.ValidateCreateOrder("کتاب", 1);

        Assert.Equal("کتاب", item);
    }

    [Fact]
    public void OrderStore_AfterCreate_ReturnsTheSameOrder()
    {
        var store = new OrderStore();

        var created = store.Create("notebook", 2);

        Assert.Equal(created, store.Get(created.Id));
        Assert.StartsWith("ord_", created.Id);
        Assert.Equal(Orders.StatusCreated, created.Status);
    }

    [Fact]
    public void OrderStore_WhenIdIsUnknown_ReturnsNull()
    {
        var store = new OrderStore();

        Assert.Null(store.Get("ord_does_not_exist"));
    }

    [Fact]
    public void OrderStore_GivesEachOrderItsOwnId()
    {
        var store = new OrderStore();

        Assert.NotEqual(store.Create("book", 1).Id, store.Create("book", 1).Id);
    }
}
