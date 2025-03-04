﻿using Customers.Application.Abstractions;
using Customers.Domain.Models;
using Customers.Infrastructure.Persistence;
using FluentAssertions;
using Library.TestHelpers;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitTests.Customers.Helpers;
using Xunit;

namespace UnitTests.Customers.Infrastructure.Persistence
{
    [Trait("customers", "infrastructure")]
    public class CustomerRepositoryTests
    {
        private ICustomerRepository _sut;

        [Fact]
        public async Task ExistAsync_ForExistingCustomer_Should_Return_True()
        {
            // arrange
            var existinCustomer = InternalFakes.Customers.Valid().Generate();
            using var customersContext = CustomersContextMockBuilder
                .Create().Customers(existinCustomer).Build();
            _sut = new CustomerRepository(customersContext);

            // act
            var result = await _sut.ExistAsync(existinCustomer.Cpf, default);

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistAsync_ForNonExistingCustomer_Should_Return_False()
        {
            // arrange
            var nonExistingCpf = Fakes.CPFs.Valid().Generate();
            using var customersContext = CustomersContextMockBuilder
                .Create().Customers().Build();
            _sut = new CustomerRepository(customersContext);

            // act
            var result = await _sut.ExistAsync(nonExistingCpf, default);

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAsync_ForExistingCustomer_Should_Return_MathingCustomer()
        {
            // arrange
            var existingCustomer = InternalFakes.Customers.Valid().Generate();
            using var customersContext = CustomersContextMockBuilder
                .Create().Customers(existingCustomer).Build();
            _sut = new CustomerRepository(customersContext);

            // act
            var result = await _sut.GetAsync(existingCustomer.Cpf, default);

            // assert
            result.Should().NotBeNull().And.BeOfType<Customer>().And.BeSameAs(existingCustomer);
        }

        [Fact]
        public async Task GetAsync_ForNonExistingCustomer_Should_Return_NullCustomer()
        {
            // arrange
            using var customersContext = CustomersContextMockBuilder
                .Create().Customers().Build();
            _sut = new CustomerRepository(customersContext);

            // act
            var result = await _sut.GetAsync(Fakes.CPFs.Valid().Generate().Value, default);

            // assert
            result.Should().NotBeNull().And.BeOfType<NullCustomer>().And.BeSameAs(Customer.Null);
        }

        [Fact]
        public async Task GetAllAsync_With_PersistedCustomers_Should_Return_SameCustomersFromDatabase()
        {
            // arrange
            var expectedCustomers = InternalFakes.Customers.Valid().Generate(2);
            using var customersContext = CustomersContextMockBuilder
                .Create().Customers(expectedCustomers.ToArray()).Build();
            _sut = new CustomerRepository(customersContext);

            // act
            var result = await _sut.GetAllAsync();

            // assert
            result.Should().NotBeNull()
                .And.BeOfType<List<Customer>>()
                .And.BeEquivalentTo(expectedCustomers);
        }

        [Fact]
        public async Task GetAllAsync_Without_PersistedCustomers_Should_Return_EmptyList()
        {
            // arrange
            using var customersContext = CustomersContextMockBuilder
                .Create().Customers().Build();
            _sut = new CustomerRepository(customersContext);

            // act
            var result = await _sut.GetAllAsync();

            // assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task InsertAsync_NewCustomer_Should_Succeed()
        {
            // arrange
            var expectedCustomer = InternalFakes.Customers.Valid().Generate();
            var builder = CustomersContextMockBuilder.Create()
                .Customers().AddCustomer(expectedCustomer);
            using var customersContext = builder.Build();
            _sut = new CustomerRepository(customersContext);

            // act
            await _sut.InsertAsync(expectedCustomer, default);

            // assert
            builder.Verify(x => x.Customers.AddAsync(expectedCustomer, default), Times.Once());
        }
    }
}
