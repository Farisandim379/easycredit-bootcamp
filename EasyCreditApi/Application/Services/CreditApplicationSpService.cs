using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationSpService : ICreditApplicationSpService
{
    private static readonly int[] ValidTenors = [6, 12, 24, 36];

    private readonly ICreditApplicationSpRepository _repository;
    private readonly ICustomerRepository _customerRepository;

    public CreditApplicationSpService(
        ICreditApplicationSpRepository repository,
        ICustomerRepository customerRepository)
    {
        _repository = repository;
        _customerRepository = customerRepository;
    }

    public async Task<(IReadOnlyList<CreditApplication> Items, int Total)> GetAllAsync(
        string? status,
        string? keyword,
        int page,
        int size)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var apps = (await _repository.GetAllAsync(status, keyword)).ToList();

        // Cross-DB decoupled lookup: ambil nama customer dari PostgreSQL
        var customerIds = apps.Select(a => a.CustomerId).Distinct().ToList();
        var customers = (await _customerRepository.GetByIdsAsync(customerIds))
            .ToDictionary(c => c.CustomerId, c => c.Name);

        foreach (var app in apps)
        {
            app.CustomerName = customers.TryGetValue(app.CustomerId, out var name)
                ? name
                : $"Customer #{app.CustomerId}";
        }

        // Filter keyword customer name jika ada
        var filtered = apps.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            filtered = filtered.Where(a =>
                (a.CustomerName != null && a.CustomerName.ToLowerInvariant().Contains(kw)) ||
                a.ApplicationId.ToString().Contains(kw));
        }

        var list = filtered.ToList();
        var total = list.Count;
        var pagedItems = list.Skip((page - 1) * size).Take(size).ToList();

        return (pagedItems, total);
    }

    public async Task<ResCreditApplicationDetailDto?> GetByIdAsync(int id)
    {
        var app = await _repository.GetByIdAsync(id);
        if (app == null)
            return null;

        var customer = await _customerRepository.GetByIdAsync(app.CustomerId);

        return new ResCreditApplicationDetailDto
        {
            ApplicationId = app.ApplicationId,
            CustomerId = app.CustomerId,
            CustomerName = customer?.Name ?? $"Customer #{app.CustomerId}",
            CustomerNik = customer?.Nik,
            CustomerPhone = customer?.Phone,
            CustomerEmail = customer?.Email,
            CustomerAddress = customer?.Address,
            Amount = app.Amount,
            TenorMonths = app.TenorMonths,
            InterestRate = app.InterestRate,
            CreditScore = app.CreditScore,
            Status = app.Status ?? "PENDING",
            ApplicationDate = app.ApplicationDate,
            ApprovedBy = app.ApprovedBy,
            ApprovedAt = app.ApprovedAt,
            Installments = app.Installments.Select(i => new ResInstallmentDto
            {
                InstallmentNo = i.InstallmentNo,
                DueDate = i.DueDate,
                InstallmentAmount = i.InstallmentAmount,
                Status = i.Status
            }).ToList()
        };
    }

    public async Task<IEnumerable<ResInstallmentDto>> PreviewInstallmentAsync(ReqPreviewInstallmentDto dto)
    {
        ValidateRules(dto.Amount, dto.TenorMonths);

        var installments = await _repository.CalculateInstallmentsAsync(dto.Amount, dto.TenorMonths);

        return installments.Select(i => new ResInstallmentDto
        {
            InstallmentNo = i.InstallmentNo,
            DueDate = i.DueDate,
            InstallmentAmount = i.InstallmentAmount,
            Status = i.Status
        });
    }

    public async Task<CreditApplication> CreateAsync(ReqCreateApplicationDto dto)
    {
        ValidateRules(dto.Amount, dto.TenorMonths);

        // Validasi customerId ada di PostgreSQL sebelum insert ke Oracle
        var customerExists = await _customerRepository.ExistsAsync(dto.CustomerId);
        if (!customerExists)
        {
            throw new ExceptionMiddleware.ValidationException($"Customer dengan ID {dto.CustomerId} tidak ditemukan di database.");
        }

        // Kalkulasi cicilan via Stored Procedure Oracle SP_CALCULATE_INSTALLMENT
        var installments = (await _repository.CalculateInstallmentsAsync(dto.Amount, dto.TenorMonths)).ToList();

        var application = new CreditApplication
        {
            CustomerId = dto.CustomerId,
            Amount = dto.Amount,
            TenorMonths = dto.TenorMonths,
            Status = "PENDING",
            InterestRate = 10,
            CreditScore = 700,
            ApplicationDate = DateTime.UtcNow
        };

        // Simpan header TRN_CREDIT_APPLICATION + semua TRN_CREDIT_INSTALLMENT dalam 1 transaksi Oracle
        var (status, message, id) = await _repository.CreateTransactionalAsync(application, installments);

        if (status != "SUCCESS")
        {
            throw new InvalidOperationException($"Gagal menyimpan pengajuan kredit: {message}");
        }

        application.ApplicationId = id;
        application.Installments = installments;
        return application;
    }

    public async Task<string> ApproveAsync(int applicationId, string approver)
    {
        var (status, message) = await _repository.ApproveAsync(applicationId, approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ValidationException(message), // 400 code key
            _ => throw new InvalidOperationException(message)
        };
    }

    public async Task<string> RejectAsync(int applicationId, string approver)
    {
        var (status, message) = await _repository.RejectAsync(applicationId, approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ValidationException(message), // 400 code key
            _ => throw new InvalidOperationException(message)
        };
    }

    private static void ValidateRules(decimal amount, int tenorMonths)
    {
        if (amount < 1000000 || amount > 50000000)
        {
            throw new ExceptionMiddleware.ValidationException("Jumlah pinjaman harus antara Rp 1.000.000 dan Rp 50.000.000.");
        }

        if (!ValidTenors.Contains(tenorMonths))
        {
            throw new ExceptionMiddleware.ValidationException("Tenor pinjaman harus 6, 12, 24, atau 36 bulan.");
        }
    }
}
