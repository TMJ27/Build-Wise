using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Requests.ViewModels;

public sealed class RequestInputViewModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(100)]
    public string Customer { get; set; } = "";

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [Required]
    public string Category { get; set; } = "";

    [Required, StringLength(200)]
    public string Location { get; set; } = "";

    [Required, StringLength(3000), MinLength(20, ErrorMessage = "Add at least 20 characters about the work required.")]
    public string Description { get; set; } = "";
}
