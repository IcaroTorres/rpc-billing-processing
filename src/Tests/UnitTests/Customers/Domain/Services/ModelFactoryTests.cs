﻿using Customers.Domain.Models;
using Customers.Domain.Services;
using FluentAssertions;
using Library.Results;
using Library.TestHelpers;
using Xunit;

namespace UnitTests.Customers.Domain.Services
{
    [Trait("customers", "domain")]
    public class ModelFactoryTests
    {
        [Fact]
        public void CreateCustomer_Should_Create_Customer_With_PropsMatching_InUpperCase()
        {
            // arrange
            var sut = new ModelFactory();
            var expectedCpf = Fakes.CPFs.Valid().Generate();
            var validName = Fakes.Names.Valid;
            var validState = Fakes.States.Valid;
            var expectedName = validName.ToUpperInvariant();
            var expectedState = validState.ToUpperInvariant();

            // act
            var result = sut.CreateCustomer(expectedCpf.ToString(), validName, validState);

            // assert
            result.Should().NotBeNull().And.BeOfType<Customer>().And.NotBeAssignableTo<INull>();
            result.Cpf.Should().Be(expectedCpf);
            result.Name.Should().NotBeNullOrEmpty().And.Be(expectedName);
            result.State.Should().NotBeNullOrEmpty().And.Be(expectedState);
        }
    }
}
