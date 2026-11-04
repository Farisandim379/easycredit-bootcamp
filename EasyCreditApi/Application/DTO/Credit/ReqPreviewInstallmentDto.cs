using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Credit;

public class ReqPreviewInstallmentDto
{
    [Required(ErrorMessage = "Jumlah pinjaman wajib diisi.")]
    [Range(1000000, 50000000, ErrorMessage = "Jumlah pinjaman harus antara Rp 1.000.000 sampai Rp 50.000.000.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Tenor pinjaman wajib dipilih.")]
    [AllowedValues(6, 12, 24, 36, ErrorMessage = "Tenor harus 6, 12, 24, atau 36 bulan.")]
    public int TenorMonths { get; set; }
}
