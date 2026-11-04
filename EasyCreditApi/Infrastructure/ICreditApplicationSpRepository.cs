using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICreditApplicationSpRepository
{
    Task<IEnumerable<CreditApplication>> GetAllAsync(string? status, string? keyword);
    Task<CreditApplication?> GetByIdAsync(int id);
    Task<IEnumerable<CreditInstallment>> CalculateInstallmentsAsync(decimal amount, int tenorMonths);
    Task<(string Status, string Message, int ApplicationId)> CreateTransactionalAsync(CreditApplication application, IEnumerable<CreditInstallment> installments);
    Task<(string Status, string Message)> ApproveAsync(int applicationId, string approver);
    Task<(string Status, string Message)> RejectAsync(int applicationId, string approver);
}
