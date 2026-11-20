using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Customers.ViewModels;

public sealed class CustomerInputViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "Individual";

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [Phone, StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
