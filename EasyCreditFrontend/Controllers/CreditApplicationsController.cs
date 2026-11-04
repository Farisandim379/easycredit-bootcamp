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
    public async Task<IActionResult> Create()
    {
        await LoadCustomersToViewBag();
        return View(new CreateApplicationViewModel());
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
            TempData["Success"] = "Pengajuan pinjaman berhasil dibuat.";
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
        var approverName = string.IsNullOrWhiteSpace(approver) ? UserName : approver.Trim();
        var payload = new { approver = approverName };
        var response = await _apiClient.PostAsync<object>($"/api/credit-applications/{id}/approve", payload);

        return Json(new
        {
            status = response?.IsSuccess ?? false,
            message = response?.Message ?? "Gagal menyetujui pengajuan."
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

        var appRes = await _apiClient.GetAsync<TotalItemsData<ApplicationListItemsDto>>("/api/credit-applications");
        var allApps = appRes?.Data?.Items ?? new List<ApplicationListItemsDto>();

        var custRes = await _apiClient.GetAsync<PageData<CustomerDto>>("/api/customers?page=1&size=100");
        var customers = (custRes?.Data?.Items ?? new List<CustomerDto>())
            .ToDictionary(c => c.CustomerId, c => c.Name);

        var filtered = allApps.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered.Select(a => new ApplicationRowViewModel
        {
            ApplicationId = a.ApplicationId,
            CustomerId = a.CustomerId,
            CustomerName = customers.TryGetValue(a.CustomerId, out var name) ? name : $"Customer #{a.CustomerId}",
            Amount = a.Amount,
            TenorMonths = a.TenorMonths,
            Status = a.Status ?? "SUBMITTED",
            ApprovedBy = a.ApprovedBy,
            ApprovedAt = a.ApprovedAt
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            rows = rows.Where(r => r.CustomerName.ToLowerInvariant().Contains(kw) ||
                                   r.ApplicationId.ToString().Contains(kw)).ToList();
        }

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)size);
        var pagedItems = rows.Skip((page - 1) * size).Take(size).ToList();

        return new ApplicationIndexViewModel
        {
            Items = pagedItems,
            Status = status,
            Keyword = keyword,
            Page = page,
            Size = size,
            Total = total,
            TotalPages = totalPages
        };
    }
}
