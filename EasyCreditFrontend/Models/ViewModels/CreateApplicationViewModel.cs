using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

public class CreateApplicationViewModel
{
    [Required(ErrorMessage = "Pilih nasabah")]
    [Display(Name = "Nasabah")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Jumlah pinjaman wajib diisi")]
    [Range(1000000, 50000000, ErrorMessage = "Pinjaman antara Rp 1.000.000 - Rp 50.000.000")]
    [Display(Name = "Jumlah Pinjaman (Rp)")]
    public decimal Amount { get; set; } = 50000000;

    [Required(ErrorMessage = "Tenor wajib dipilih")]
    [AllowedValues(6, 12, 24, 36, ErrorMessage = "Tenor harus 6, 12, 24, atau 36 bulan")]
    [Display(Name = "Tenor Pinjaman")]
    public int TenorMonths { get; set; } = 12;
}
