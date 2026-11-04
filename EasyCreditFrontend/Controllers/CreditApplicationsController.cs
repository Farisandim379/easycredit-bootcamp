using EasyCreditFrontend.Models;
using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyCreditFrontend.Controllers;

public class CreditApplicationsController : BaseController
{
    private readonly ApiClient _apiClient;

    public CreditApplicationsController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? keyword, int page = 1, int size = 10)
    {
        var model = await BuildIndexAsync(status, keyword, page, size);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var response = await _apiClient.GetAsync<ApplicationDetailViewModel>($"/api/credit-applications/{id}");
        if (response == null || !response.IsSuccess || response.Data == null)
        {
            TempData["Error"] = response?.Message ?? $"Pengajuan dengan ID #{id} tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.IsAdmin = IsAdmin;
        ViewBag.UserName = UserName;
        return View(response.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCustomersToViewBag();
        return View(new CreateApplicationViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> PreviewInstallment([FromBody] PreviewInstallmentRequest req)
    {
        if (req == null || req.Amount < 1000000 || req.Amount > 50000000 || !new[] { 6, 12, 24, 36 }.Contains(req.TenorMonths))
        {
            return Json(new
            {
                status = false,
                message = "Nominal harus antara Rp 1.000.000 - Rp 50.000.000 dan tenor harus 6, 12, 24, atau 36 bulan."
            });
        }

        var response = await _apiClient.PostAsync<List<InstallmentItemViewModel>>(
            "/api/credit-applications/preview-installment",
            new { amount = req.Amount, tenorMonths = req.TenorMonths });

        return Json(new
        {
            status = response?.IsSuccess ?? false,
            message = response?.Message ?? "Gagal menghitung simulasi cicilan.",
            data = response?.Data ?? new List<InstallmentItemViewModel>()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateApplicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomersToViewBag();
            return View(model);
        }

        var payload = new
        {
            customerId = model.CustomerId,
            amount = model.Amount,
            tenorMonths = model.TenorMonths
        };

        var response = await _apiClient.PostAsync<object>("/api/credit-applications", payload);

        if (response is { IsSuccess: true })
        {
            TempData["Success"] = "Pengajuan pinjaman dan skema cicilan berhasil dibuat.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, response?.Message ?? "Gagal membuat pengajuan pinjaman.");
        await LoadCustomersToViewBag();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? approver)
    {
        if (!IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                status = false,
                message = "Akses ditolak: STAFF tidak memiliki izin untuk menyetujui pengajuan kredit (403 Forbidden)."
            });
        }

        var approverName = string.IsNullOrWhiteSpace(approver) ? UserName : approver.Trim();
        var payload = new { approver = approverName };
        var response = await _apiClient.PutAsync<object>($"/api/credit-applications/{id}/approve", payload);

        return Json(new
        {
            status = response?.IsSuccess ?? false,
            message = response?.Message ?? "Gagal menyetujui pengajuan."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? approver)
    {
        if (!IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                status = false,
                message = "Akses ditolak: STAFF tidak memiliki izin untuk menolak pengajuan kredit (403 Forbidden)."
            });
        }

        var approverName = string.IsNullOrWhiteSpace(approver) ? UserName : approver.Trim();
        var payload = new { approver = approverName };
        var response = await _apiClient.PutAsync<object>($"/api/credit-applications/{id}/reject", payload);

        return Json(new
        {
            status = response?.IsSuccess ?? false,
            message = response?.Message ?? "Gagal menolak pengajuan."
        });
    }

    private async Task LoadCustomersToViewBag()
    {
        var custRes = await _apiClient.GetAsync<PageData<CustomerDto>>("/api/customers?page=1&size=100");
        var list = custRes?.Data?.Items ?? new List<CustomerDto>();

        ViewBag.CustomerList = list.Select(c => new SelectListItem
        {
            Value = c.CustomerId.ToString(),
            Text = $"{c.Name} (NIK: {c.Nik})"
        }).ToList();
    }

    private async Task<ApplicationIndexViewModel> BuildIndexAsync(string? status, string? keyword, int page, int size)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var queryUrl = $"/api/credit-applications?page={page}&size={size}";
        if (!string.IsNullOrWhiteSpace(status))
            queryUrl += $"&status={Uri.EscapeDataString(status.Trim())}";
        if (!string.IsNullOrWhiteSpace(keyword))
            queryUrl += $"&keyword={Uri.EscapeDataString(keyword.Trim())}";

        var appRes = await _apiClient.GetAsync<PageData<ApplicationListItemsDto>>(queryUrl);
        var pagedData = appRes?.Data;
        var items = pagedData?.Items ?? new List<ApplicationListItemsDto>();
        var total = pagedData?.Total ?? items.Count;

        var rows = items.Select(a => new ApplicationRowViewModel
        {
            ApplicationId = a.ApplicationId,
            CustomerId = a.CustomerId,
            CustomerName = !string.IsNullOrWhiteSpace(a.CustomerName) ? a.CustomerName : $"Customer #{a.CustomerId}",
            Amount = a.Amount,
            TenorMonths = a.TenorMonths,
            Status = a.Status ?? "PENDING",
            ApplicationDate = a.ApplicationDate,
            ApprovedBy = a.ApprovedBy,
            ApprovedAt = a.ApprovedAt
        }).ToList();

        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)size);

        return new ApplicationIndexViewModel
        {
            Items = rows,
            Status = status,
            Keyword = keyword,
            Page = page,
            Size = size,
            Total = total,
            TotalPages = totalPages
        };
    }
}

public class PreviewInstallmentRequest
{
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
}
