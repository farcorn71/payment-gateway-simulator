using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Common.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);
        Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    }
}
