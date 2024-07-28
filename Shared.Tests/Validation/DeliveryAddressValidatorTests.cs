using FluentValidation.TestHelper;
using Shared.Entities;
using Shared.Validation;

namespace Shared.Tests.Validation;

public class DeliveryAddressValidatorTests
{
    private readonly DeliveryAddressValidator _validator;

        public DeliveryAddressValidatorTests()
        {
            _validator = new DeliveryAddressValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Type_Is_Empty()
        {
            var address = new DeliveryAddress { Type = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.Type)
                .WithErrorMessage("Der Type darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_Type_Is_Too_Long()
        {
            var address = new DeliveryAddress { Type = new string('a', 31) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.Type)
                .WithErrorMessage("Die Länge des Type muss zwischen 1 und 30 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_SalutationCode_Is_Empty()
        {
            var address = new DeliveryAddress { SalutationCode = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.SalutationCode)
                .WithErrorMessage("Der SalutationCode darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_SalutationCode_Is_Too_Long()
        {
            var address = new DeliveryAddress { SalutationCode = new string('a', 31) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.SalutationCode)
                .WithErrorMessage("Die Länge des SalutationCode muss zwischen 1 und 30 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_FirstName_Is_Empty()
        {
            var address = new DeliveryAddress { FirstName = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.FirstName)
                .WithErrorMessage("Der FirstName darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_FirstName_Is_Too_Long()
        {
            var address = new DeliveryAddress { FirstName = new string('a', 151) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.FirstName)
                .WithErrorMessage("Die Länge des FirstName muss zwischen 1 und 150 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_LastName_Is_Empty()
        {
            var address = new DeliveryAddress { LastName = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.LastName)
                .WithErrorMessage("Der LastName darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_LastName_Is_Too_Long()
        {
            var address = new DeliveryAddress { LastName = new string('a', 151) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.LastName)
                .WithErrorMessage("Die Länge des LastName muss zwischen 1 und 150 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_StreetName_Is_Too_Long()
        {
            var address = new DeliveryAddress { StreetName = new string('a', 101) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.StreetName)
                .WithErrorMessage("Die Länge von streetName darf nur maximal 100 Zeichen sein.");
        }

        [Fact]
        public void Should_Have_Error_When_StreetNumber_Is_Too_Long()
        {
            var address = new DeliveryAddress { StreetNumber = new string('a', 101) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.StreetNumber)
                .WithErrorMessage("Die Länge von streetNumber darf nur maximal 100 Zeichen sein.");
        }

        [Fact]
        public void Should_Have_Error_When_PostalCode_Is_Empty()
        {
            var address = new DeliveryAddress { PostalCode = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.PostalCode)
                .WithErrorMessage("Der PostalCode darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_PostalCode_Is_Too_Long()
        {
            var address = new DeliveryAddress { PostalCode = new string('a', 11) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.PostalCode)
                .WithErrorMessage("Die Länge des PostalCode muss zwischen 1 und 10 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_Town_Is_Empty()
        {
            var address = new DeliveryAddress { Town = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.Town)
                .WithErrorMessage("Die Town darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_Town_Is_Too_Long()
        {
            var address = new DeliveryAddress { Town = new string('a', 51) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.Town)
                .WithErrorMessage("Die Länge der Town muss zwischen 1 und 50 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_CountryIsoCode_Is_Empty()
        {
            var address = new DeliveryAddress { CountryIsoCode = string.Empty };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.CountryIsoCode)
                .WithErrorMessage("Der CountryIsoCode darf nicht leer sein.");
        }

        [Fact]
        public void Should_Have_Error_When_CountryIsoCode_Is_Too_Long()
        {
            var address = new DeliveryAddress { CountryIsoCode = new string('a', 4) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.CountryIsoCode)
                .WithErrorMessage("Die Länge des CountryIsoCode muss zwischen 1 und 3 Zeichen liegen.");
        }

        [Fact]
        public void Should_Have_Error_When_Remarks_Is_Too_Long()
        {
            var address = new DeliveryAddress { Remarks = new string('a', 201) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.Remarks)
                .WithErrorMessage("Die Länge der Remarks darf maximal 200 Zeichen betragen.");
        }

        [Fact]
        public void Should_Have_Error_When_PackstationNumber_Is_Too_Long()
        {
            var address = new DeliveryAddress { PackstationNumber = new string('a', 31) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.PackstationNumber)
                .WithErrorMessage("Die Länge der PackstationNumber darf maximal 30 Zeichen betragen.");
        }

        [Fact]
        public void Should_Have_Error_When_PostNumber_Is_Too_Long()
        {
            var address = new DeliveryAddress { PostNumber = new string('a', 11) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.PostNumber)
                .WithErrorMessage("Die Länge der PostNumber darf maximal 10 Zeichen betragen.");
        }

        [Fact]
        public void Should_Have_Error_When_PostOfficeNumber_Is_Too_Long()
        {
            var address = new DeliveryAddress { PostOfficeNumber = new string('a', 11) };
            var result = _validator.TestValidate(address);
            result.ShouldHaveValidationErrorFor(a => a.PostOfficeNumber)
                .WithErrorMessage("Die Länge der PostOfficeNumber darf maximal 10 Zeichen betragen.");
        }
}