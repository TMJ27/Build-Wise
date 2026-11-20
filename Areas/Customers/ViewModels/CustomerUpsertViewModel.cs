using System.ComponentModel.DataAnnotations;
using BuildWise.Lib.Enums;

namespace BuildWise.Web.Areas.Customers.ViewModels;

public class CustomerUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required, StringLength(50)]
    public string Address { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(ClientType))]
    public ClientType? ClientTypeId { get; set; }

    public bool IsActive { get; set; } = true;
}
