using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/credit-applications")]
[Authorize]
public class CreditApplicationsController : ControllerBase
{
    private readonly ICreditApplicationSpService _service;

    public CreditApplicationsController(ICreditApplicationSpService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var (items, total) = await _service.GetAllAsync(status, keyword, page, size);
        return Ok(ApiResponse<object>.Success(new { page, size, total, items }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var application = await _service.GetByIdAsync(id);
        if (application == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Pengajuan kredit dengan ID #{id} tidak ditemukan."));
        }

        return Ok(ApiResponse<ResCreditApplicationDetailDto>.Success(application));
    }

    [HttpPost("preview-installment")]
    public async Task<IActionResult> PreviewInstallment([FromBody] ReqPreviewInstallmentDto dto)
    {
        var items = await _service.PreviewInstallmentAsync(dto);
        return Ok(ApiResponse<IEnumerable<ResInstallmentDto>>.Success(items, "Simulasi cicilan berhasil dihitung."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateApplicationDto dto)
    {
        var application = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = application.ApplicationId },
            ApiResponse<object>.Success(application, "Pengajuan pinjaman berhasil dibuat."));
    }

    [HttpPut("{id:int}/approve")]
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "ADMIN,Admin")]
    public async Task<IActionResult> Approve(int id, [FromBody] ReqApproveApplicationDto? dto)
    {
        var approver = !string.IsNullOrWhiteSpace(dto?.Approver) ? dto.Approver : (User.Identity?.Name ?? "ADMIN");
        var message = await _service.ApproveAsync(id, approver);

        return Ok(ApiResponse<object>.Success(
            new { applicationId = id, status = "APPROVED" },
            message));
    }

    [HttpPut("{id:int}/reject")]
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "ADMIN,Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReqApproveApplicationDto? dto)
    {
        var approver = !string.IsNullOrWhiteSpace(dto?.Approver) ? dto.Approver : (User.Identity?.Name ?? "ADMIN");
        var message = await _service.RejectAsync(id, approver);

        return Ok(ApiResponse<object>.Success(
            new { applicationId = id, status = "REJECTED" },
            message));
    }
}
