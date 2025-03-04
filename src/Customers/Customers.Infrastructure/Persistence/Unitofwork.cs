﻿using Customers.Application.Abstractions;
using Library.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Infrastructure.Persistence
{
    /// <inheritdoc cref="IUnitofwork" />
    public class Unitofwork : IUnitofwork
    {
        private readonly DbContext _context;
        private IDbContextTransaction _transaction;
        private bool _disposed;
        private bool _transactionOpen = false;

        public Unitofwork(DbContext context)
        {
            _context = context;
        }

        public bool HasTransactionOpen() => _transactionOpen;

        public IUnitofwork BeginTransaction()
        {
            var unit = _transaction is null ? this : new Unitofwork(_context);
            return SetTransaction(unit);
        }

        public async Task<IResult> CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var changes = await _context.SaveChangesAsync(cancellationToken);
                _transaction?.CommitAsync(cancellationToken);
                _transactionOpen = false;
                return new SuccessResult(changes);
            }
            catch (Exception exception)
            {
                await RollbackAsync(cancellationToken);
                return new FailResult(StatusCodes.Status409Conflict,
                    new DbUpdateException("Failed handling data layer operation.", exception).ExtractMessages());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (!_transactionOpen) return;

            await _transaction?.RollbackAsync(cancellationToken);
            _transactionOpen = false;
        }

        private Unitofwork SetTransaction(Unitofwork unit)
        {
            unit._transaction = _context.Database.BeginTransaction();
            unit._transactionOpen = true;
            return unit;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _context.Dispose();
                _transaction?.Dispose();
            }
            _transactionOpen = false;
            _disposed = true;
        }
    }
}
