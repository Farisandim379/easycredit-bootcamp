namespace EasyCreditApi.Application.DTO.Credit;

public class ResCreditApplicationDetailDto
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerNik { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal Amount { get; set; }
    public int TenorMonths { get; set; }
    public decimal? InterestRate { get; set; }
    public int? CreditScore { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ApplicationDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<ResInstallmentDto> Installments { get; set; } = new();
}
