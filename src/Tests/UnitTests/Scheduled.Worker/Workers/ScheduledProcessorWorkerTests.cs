﻿using FluentAssertions;
using Library.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Processing.Scheduled.Worker;
using Processing.Scheduled.Worker.Models;
using Processing.Scheduled.Worker.Services;
using Processing.Scheduled.Worker.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnitTests.Scheduled.Worker.Helpers;
using Xunit;

namespace UnitTests.Scheduled.Worker.Workers
{
    [Trait("processing", "scheduled-worker")]
    public class ScheduledProcessorWorkerTests
    {
        private readonly Mock<IRpcClient<List<Customer>>> _customerClientMock;
        private readonly Mock<IRpcClient<List<Billing>>> _billingClientMock;
        private readonly Mock<IAmountProcessor> _amountProcessorMock;
        private readonly IComparer<ICpfCarrier> _comparer;
        private readonly Mock<ScheduledProcessorSettings> _settingsMock;
        private readonly Mock<ILogger<ScheduledProcessorWorker>> _loggerMock;
        private readonly ScheduledProcessorWorker _sut;

        public ScheduledProcessorWorkerTests()
        {
            _customerClientMock = new Mock<IRpcClient<List<Customer>>>();
            _billingClientMock = new Mock<IRpcClient<List<Billing>>>();
            _amountProcessorMock = new Mock<IAmountProcessor>();
            _comparer = new CpfCarrierComparer();
            _settingsMock = new Mock<ScheduledProcessorSettings>();
            _loggerMock = new Mock<ILogger<ScheduledProcessorWorker>>();
            _sut = new ScheduledProcessorWorker(
                _customerClientMock.Object,
                _billingClientMock.Object,
                _amountProcessorMock.Object,
                _comparer,
                _settingsMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task FetchBatch_Should_FetchCustomersAndBillings_WithCustomersSorted_ToMatchAndProcessAsync()
        {
            // arrange
            const int billingsCount = 10;
            var batch = new ProcessBatch();
            var billingResponse = InternalFakes.Billings.Valid().Generate(billingsCount);
            var clientResponse = new List<Customer>();
            var expectedCustomers = new List<ICpfCarrier>();
            foreach (var billing in billingResponse)
            {
                var customer = new Customer { Cpf = billing.Cpf };
                expectedCustomers.Add(customer);
                clientResponse.Add(customer);
            }
            _billingClientMock.Setup(x => x.CallProcedure(batch.Billings)).Returns(billingResponse);
            _customerClientMock.Setup(x => x.CallProcedure("")).Returns(clientResponse);

            // act
            var result = await _sut.FetchBatchAsync(batch);

            // assert
            result.Should().NotBeNull().And.BeOfType<ProcessBatch>();
            result.Billings.Should().NotBeNull().And.BeEquivalentTo(billingResponse);
            result.Customers.Should().NotBeNull().And.BeEquivalentTo(expectedCustomers);
        }

        [Fact]
        public void ProcessBatch_Shuold_Return_BillingsProcessed_ForMatchingCustomers()
        {
            // arrange on constructor
            const int billingsCount = 100;
            const int processableCount = 20;
            var billings = InternalFakes.Billings.Valid().Generate(billingsCount);
            var billingArray = billings.ToArray();
            var processableCpfs = billingArray[..processableCount]
                .Select((x, index) =>
                {
                    var customer = (ICpfCarrier)new Customer { Cpf = x.Cpf };
                    _amountProcessorMock.Setup(y => y.Process(customer, billingArray[index]))
                        .Returns(new Billing
                        {
                            Id = billingArray[index].Id,
                            Cpf = x.Cpf,
                            Amount = 100,
                            ProcessedAt = DateTime.UtcNow
                        });
                    return customer;
                }).ToList();
            var unprocessableCpfs = billingArray.Except(processableCpfs).ToList();
            processableCpfs.Sort(_comparer);
            var batch = new ProcessBatch { Customers = processableCpfs, Billings = billings };

            // act
            var result = _sut.ProcessBatch(batch);

            // assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Billings.Should()
                .HaveCount(billingsCount).And
                .OnlyContain(x =>
                    unprocessableCpfs.Any(a => a.Cpf == x.Cpf) && x.ProcessedAt == null ||
                    processableCpfs.Any(x => x.Cpf == x.Cpf) && x.ProcessedAt != null).And
                .Match(items => items.Count(x => x.ProcessedAt == null) == billingsCount - processableCount).And
                .Match(items => items.Count(x => x.ProcessedAt != null) == processableCount);
        }

        [Fact]
        public void ProcessBatch_Without_Customers_Shuold_ReturnInstance_And_SkipProcess()
        {
            // arrange on constructor
            const int billingsCount = 2;
            var billings = InternalFakes.Billings.Valid().Generate(billingsCount);
            var batch = new ProcessBatch { Customers = new List<ICpfCarrier>(), Billings = billings };

            // act
            var result = _sut.ProcessBatch(batch);

            // assert
            result.Should().NotBeNull().And.Be(batch);
            result.Billings.Should().HaveCount(billingsCount).And.OnlyContain(x => x.ProcessedAt == null);
        }

        [Fact]
        public void ProcessBatchJoin_Shuold_Return_BillingsProcessed_ForMatchingCustomers()
        {
            // arrange on constructor
            const int billingsCount = 100;
            const int processableCount = 20;
            var billings = InternalFakes.Billings.Valid().Generate(billingsCount);
            ICpfCarrier[] processableCpfs = new ICpfCarrier[processableCount];
            for (var i = 0; i < processableCount; i++)
            {
                processableCpfs[i] = new Customer { Cpf = billings[i].Cpf };
                _amountProcessorMock.Setup(y => y.Process(processableCpfs[i], billings[i]))
                  .Returns(new Billing
                  {
                      Id = billings[i].Id,
                      Cpf = billings[i].Cpf,
                      Amount = 100,
                      ProcessedAt = DateTime.UtcNow
                  });
            }
            var batch = new ProcessBatch { Customers = new List<ICpfCarrier>(processableCpfs), Billings = billings };

            // act
            var result = _sut.ProcessBatchJoin(batch);

            // assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Billings.Should().HaveCount(processableCount).And.OnlyContain(x => x.ProcessedAt != null);
        }

        [Fact]
        public void ProcessBatch_Without_Billings_Shuold_ReturnInstance_And_SkipProcess()
        {
            // arrange on constructor
            var customers = InternalFakes.Customers.Valid().Generate(2);
            var batch = new ProcessBatch { Customers = new List<ICpfCarrier>(customers), Billings = new List<Billing>() };

            // act
            var result = _sut.ProcessBatch(batch);

            // assert
            result.Should().NotBeNull().And.Be(batch);
            result.Billings.Should().HaveCount(0);
            result.Customers.Should().HaveCount(2);
        }

        [Fact]
        public void ProcessBatch_Without_BillingsAndCustomers_Shuold_ReturnInstance_And_SkipProcess()
        {
            // arrange on constructor
            var batch = new ProcessBatch();

            // act
            var result = _sut.ProcessBatch(batch);

            // assert
            result.Should().NotBeNull().And.Be(batch);
            result.Billings.Should().HaveCount(0);
            result.Customers.Should().HaveCount(0);
        }

        [Fact]
        public async Task DoExecute_Should_ExecuteProcess_Wait_ExpectedDelay_ResetBatchId_And_ReturnProcessedBatch()
        {
            // arrange
            const int billingsCount = 10;
            const int expectedDelay = 100;
            _settingsMock.Setup(x => x.MillisecondsScheduledTime).Returns(expectedDelay);
            var batch = new ProcessBatch();
            var previousId = batch.Id;
            var billingResponse = InternalFakes.Billings.Valid().Generate(billingsCount);
            var clientResponse = new List<Customer>();
            var expectedCustomers = new List<ICpfCarrier>();
            foreach (var billing in billingResponse)
            {
                var customer = new Customer { Cpf = billing.Cpf };
                expectedCustomers.Add(customer);
                clientResponse.Add(customer);
                _amountProcessorMock.Setup(y => y.Process(customer, billing))
                    .Returns(new Billing
                    {
                        Id = billing.Id,
                        Cpf = billing.Cpf,
                        Amount = 100,
                        ProcessedAt = DateTime.UtcNow
                    });
            }
            _billingClientMock.Setup(x => x.CallProcedure(batch.Billings)).Returns(billingResponse);
            _customerClientMock.Setup(x => x.CallProcedure("")).Returns(clientResponse);

            // act
            var result = await _sut.DoExecute(batch);

            // assert
            result.Should().NotBeNull().And.BeOfType<ProcessBatch>();
            result.Id.Should().NotBeNullOrEmpty().And.NotBe(previousId);
            result.Customers.Should().NotBeNull().And
                .BeEquivalentTo(clientResponse);
            result.Billings.Should().NotBeNull().And
                .BeEquivalentTo(billingResponse).And
                .HaveCount(billingsCount).And
                .OnlyContain(x => x.ProcessedAt != null && expectedCustomers.Any(y => y.Cpf == x.Cpf));
        }
    }
}
