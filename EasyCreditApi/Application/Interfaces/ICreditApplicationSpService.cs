using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationSpService
{
    Task<IReadOnlyList<CreditApplication>> GetAllAsync();
    Task<CreditApplication> CreateAsync(ReqCreateApplicationDto dto);
    Task<string> ApproveAsync(int applicationId, string approver);
}
