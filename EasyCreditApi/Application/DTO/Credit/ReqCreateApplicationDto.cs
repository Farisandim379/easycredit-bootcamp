using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Credit;

public class ReqCreateApplicationDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    [Range(1000000, 500000000, ErrorMessage = "Jumlah pinjaman harus antara Rp 1.000.000 sampai Rp 500.000.000")]
    public decimal Amount { get; set; }

    [Required]
    [Range(1, 120, ErrorMessage = "Tenor harus antara 1 sampai 120 bulan")]
    public int TenorMonths { get; set; }
}
