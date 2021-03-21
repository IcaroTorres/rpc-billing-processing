﻿using Customers.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Customers.Api.Infrastructure.Persistence
{
    public class CustomersContext : DbContext
    {
        public CustomersContext(DbContextOptions<CustomersContext> options) : base(options) { }

        /// <summary>
        /// For mock purposes
        /// </summary>
        protected CustomersContext()
        {
        }

        public virtual DbSet<Customer> Customers { get; set; }

        [ExcludeFromCodeCoverage]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<NullCustomer>();
            modelBuilder.ApplyConfiguration(new CustomerMap());
            base.OnModelCreating(modelBuilder);
        }
    }
}
