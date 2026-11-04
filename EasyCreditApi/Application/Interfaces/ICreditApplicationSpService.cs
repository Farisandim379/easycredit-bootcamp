using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationSpService
{
    Task<(IReadOnlyList<CreditApplication> Items, int Total)> GetAllAsync(string? status, string? keyword, int page, int size);
    Task<ResCreditApplicationDetailDto?> GetByIdAsync(int id);
    Task<IEnumerable<ResInstallmentDto>> PreviewInstallmentAsync(ReqPreviewInstallmentDto dto);
    Task<CreditApplication> CreateAsync(ReqCreateApplicationDto dto);
    Task<string> ApproveAsync(int applicationId, string approver);
    Task<string> RejectAsync(int applicationId, string approver);
}
