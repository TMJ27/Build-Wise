using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Access;

public sealed class IntegrationInputViewModel
{
    [Required, StringLength(50)]
    public string Provider { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? SenderName { get; set; }

    [EmailAddress, StringLength(256)]
    public string? SenderEmail { get; set; }

    [StringLength(253)]
    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool UseTls { get; set; } = true;
    public bool EnableWhenConnected { get; set; }

    [Range(1, long.MaxValue)]
    public long Version { get; set; }
}
