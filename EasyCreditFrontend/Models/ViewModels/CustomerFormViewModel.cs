using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

public class CustomerFormViewModel
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Nama wajib di isi")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
    [Display(Name = "Nama lengkap")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NIK wajib di isi")]
    [RegularExpression(@"^\d{16,20}$", ErrorMessage = "NIK harus 16-20 digit angka.")]
    [Display(Name = "NIK")]
    public string Nik { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Nomor Telepon Maksimal 20 karakter")]
    [Phone(ErrorMessage = "Format Nomor telepon tidak valid")]
    [Display(Name = "Nomor Telepon")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Format Email tidak valid")]
    [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Alamat maksimal 100 karakter")]
    [Display(Name = "Alamat")]
    public string? Address { get; set; }
}