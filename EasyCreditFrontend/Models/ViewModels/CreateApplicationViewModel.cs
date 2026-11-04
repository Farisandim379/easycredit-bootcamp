using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

public class CreateApplicationViewModel
{
    [Required(ErrorMessage = "Pilih nasabah")]
    [Display(Name = "Nasabah")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Jumlah pinjaman wajib diisi")]
    [Range(1000000, 500000000, ErrorMessage = "Pinjaman antara Rp 1.000.000 - Rp 500.000.000")]
    [Display(Name = "Jumlah Pinjaman (Rp)")]
    public decimal Amount { get; set; } = 5000000;

    [Required(ErrorMessage = "Tenor wajib dipilih")]
    [Range(1, 120, ErrorMessage = "Tenor antara 1 - 120 bulan")]
    [Display(Name = "Tenor (Bulan)")]
    public int TenorMonths { get; set; } = 12;
}
