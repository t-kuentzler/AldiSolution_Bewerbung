using FluentValidation.TestHelper;
using Shared.Entities;
using Shared.Validation;

namespace Shared.Tests.Validation;

public class OrderEntryValidatorTests
{
    private readonly OrderEntryValidator _validator;

    public OrderEntryValidatorTests()
    {
        _validator = new OrderEntryValidator();
    }

    [Fact]
    public void Should_Have_Error_When_EntryNumber_Is_Zero()
    {
        var orderEntry = new OrderEntry { EntryNumber = 0 };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.EntryNumber)
            .WithErrorMessage("Die EntryNumber darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_VendorProductCode_Is_Empty()
    {
        var orderEntry = new OrderEntry { VendorProductCode = string.Empty };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.VendorProductCode)
            .WithErrorMessage("Der VendorProductCode darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_AldiProductCode_Is_Empty()
    {
        var orderEntry = new OrderEntry { AldiProductCode = string.Empty };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.AldiProductCode)
            .WithErrorMessage("Der AldiProductCode darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_ProductName_Is_Empty()
    {
        var orderEntry = new OrderEntry { ProductName = string.Empty };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.ProductName)
            .WithErrorMessage("Der ProductName darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_ProductName_Is_Too_Long()
    {
        var orderEntry = new OrderEntry { ProductName = new string('a', 101) };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.ProductName)
            .WithErrorMessage("Die Länge des Produktnamens muss zwischen 1 und 100 Zeichen liegen.");
    }

    [Fact]
    public void Should_Have_Error_When_Quantity_Is_Empty()
    {
        var orderEntry = new OrderEntry { Quantity = default };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.Quantity)
            .WithErrorMessage("Die Quantity darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_CarrierCode_Is_Too_Long()
    {
        var orderEntry = new OrderEntry { CarrierCode = new string('a', 51) };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.CarrierCode)
            .WithErrorMessage("Die Länge des CarrierCode darf maximal 50 Zeichen betragen.");
    }

    [Fact]
    public void Should_Have_Error_When_AldiSuedProductCode_Is_Too_Long()
    {
        var orderEntry = new OrderEntry { AldiSuedProductCode = new string('a', 51) };
        var result = _validator.TestValidate(orderEntry);
        result.ShouldHaveValidationErrorFor(e => e.AldiSuedProductCode)
            .WithErrorMessage("Die Länge des AldiSuedProductCode darf maximal 50 Zeichen betragen.");
    }
}