using System.Collections.Concurrent;

using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Infrastructure.Repositories
{
    public sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

        public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
        {
            _payments.TryGetValue(id.Value, out var payment);
            return Task.FromResult(payment);
        }

        public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            if (!_payments.TryAdd(payment.Id.Value, payment))
            {
                throw new InvalidOperationException($"Payment with ID {payment.Id} already exists");
            }

            return Task.CompletedTask;
        }

        // For testing purposes
        public int Count => _payments.Count;
        public void Clear() => _payments.Clear();
    }
}
