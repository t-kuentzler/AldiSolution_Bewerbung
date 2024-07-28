using FluentValidation.TestHelper;
using Shared.Entities;
using Shared.Validation;

namespace Shared.Tests.Validation;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator;

    public OrderValidatorTests()
    {
        _validator = new OrderValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Empty()
    {
        var order = new Order { Code = string.Empty };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Code)
            .WithErrorMessage("Der Code darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Too_Long()
    {
        var order = new Order { Code = new string('a', 101) };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Code)
            .WithErrorMessage("Die Länge des Codes muss zwischen 1 und 100 Zeichen liegen.");
    }

    [Fact]
    public void Should_Have_Error_When_Status_Is_Empty()
    {
        var order = new Order { Status = string.Empty };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Status)
            .WithErrorMessage("Der Status darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_Status_Is_Too_Long()
    {
        var order = new Order { Status = new string('a', 31) };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Status)
            .WithErrorMessage("Die Länge des Status muss zwischen 1 und 30 Zeichen liegen.");
    }

    [Fact]
    public void Should_Have_Error_When_Created_Is_Empty()
    {
        var order = new Order { Created = default };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Created)
            .WithErrorMessage("Das Erstellungsdatum Created darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_Modified_Is_Empty()
    {
        var order = new Order { Modified = default };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Modified)
            .WithErrorMessage("Das Änderungsdatum Modified darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_AldiCustomerNumber_Is_Empty()
    {
        var order = new Order { AldiCustomerNumber = string.Empty };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.AldiCustomerNumber)
            .WithErrorMessage("Die AldiCustomerNumber darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_AldiCustomerNumber_Is_Too_Long()
    {
        var order = new Order { AldiCustomerNumber = new string('a', 51) };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.AldiCustomerNumber)
            .WithErrorMessage("Die Länge der AldiCustomerNumber muss zwischen 1 und 50 Zeichen liegen.");
    }

    [Fact]
    public void Should_Have_Error_When_EmailAddress_Is_Too_Long()
    {
        var order = new Order { EmailAddress = new string('a', 101) };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.EmailAddress)
            .WithErrorMessage("Die Länge der EmailAddress darf maximal 100 Zeichen betragen.");
    }

    [Fact]
    public void Should_Have_Error_When_Phone_Is_Too_Long()
    {
        var order = new Order { Phone = new string('a', 31) };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Phone)
            .WithErrorMessage("Die Länge von Phone darf maximal 30 Zeichen betragen.");
    }

    [Fact]
    public void Should_Have_Error_When_Language_Is_Empty()
    {
        var order = new Order { Language = string.Empty };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Language)
            .WithErrorMessage("Die Language darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_Language_Is_Not_Exactly_2_Characters()
    {
        var order = new Order { Language = "EN" };
        var result = _validator.TestValidate(order);
        result.ShouldNotHaveValidationErrorFor(o => o.Language);

        order.Language = "E";
        result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Language)
            .WithErrorMessage("Die Länge der Language muss genau 2 Zeichen betragen.");

        order.Language = "ENG";
        result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.Language)
            .WithErrorMessage("Die Länge der Language muss genau 2 Zeichen betragen.");
    }

    [Fact]
    public void Should_Have_Error_When_OrderDeliveryArea_Is_Empty()
    {
        var order = new Order { OrderDeliveryArea = string.Empty };
        var result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.OrderDeliveryArea)
            .WithErrorMessage("Die OrderDeliveryArea darf nicht leer sein.");
    }

    [Fact]
    public void Should_Have_Error_When_OrderDeliveryArea_Is_Not_Exactly_1_Character()
    {
        var order = new Order { OrderDeliveryArea = "A" };
        var result = _validator.TestValidate(order);
        result.ShouldNotHaveValidationErrorFor(o => o.OrderDeliveryArea);

        order.OrderDeliveryArea = "";
        result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.OrderDeliveryArea)
            .WithErrorMessage("Die Länge der OrderDeliveryArea muss genau 1 Zeichen betragen.");

        order.OrderDeliveryArea = "AB";
        result = _validator.TestValidate(order);
        result.ShouldHaveValidationErrorFor(o => o.OrderDeliveryArea)
            .WithErrorMessage("Die Länge der OrderDeliveryArea muss genau 1 Zeichen betragen.");
    }
}