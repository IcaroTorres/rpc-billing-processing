﻿using Bogus;
using Processing.Scheduled.Worker.Models;
using System;
using static Library.TestHelpers.Fakes;

namespace UnitTests.Scheduled.Worker.Helpers
{
    public static class InternalFakes
    {
        public static class Billings
        {
            public static Faker<Billing> Valid(ulong? cpf = null) => new Faker<Billing>()
                .RuleFor(x => x.Id, Guid.NewGuid)
                .RuleFor(x => x.Cpf, x => cpf ?? CPFs.Valid().Generate())
                .RuleFor(x => x.Amount, x => x.Random.Decimal(1, 2000));
        }

        public static class Customers
        {
            public static Faker<Customer> Valid() => new Faker<Customer>().RuleFor(x => x.Cpf, CPFs.Valid().Generate());
        }
    }
}
