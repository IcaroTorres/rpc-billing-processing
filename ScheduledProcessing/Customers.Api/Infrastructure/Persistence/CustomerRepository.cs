﻿using Customers.Api.Application.Abstractions;
using Customers.Api.Domain.Models;
using Library.Results;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Api.Infrastructure.Persistence
{
    /// <inheritdoc cref="ICustomerRepository"/>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CustomersContext _context;

        public CustomerRepository(CustomersContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistAsync(ulong id, CancellationToken token)
        {
            return await _context.Customers.AnyAsync(x => x.Cpf.Equals(id));
        }

        public async Task<Customer> GetAsync(ulong id, CancellationToken token)
        {
            return await _context.Customers.SingleOrDefaultAsync(x => x.Cpf.Equals(id)) ?? Customer.Null;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task InsertAsync(Customer customer, CancellationToken token)
        {
            if (customer is INull) return;
            await _context.Customers.AddAsync(customer, token);
        }
    }
}
