using System.Net;
using BuildWise.Web.Areas.Quotations.ViewModels;

namespace BuildWise.Web.Services.Email;

public static class QuotationEmailTemplate
{
    public const string LogoPlaceholder = "<!--company-logo-->";

    public static string Create(QuotationDocumentViewModel document, string message)
    {
        var revision = document.Revision;
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var content = Encode(message.Trim()).Replace("\r\n", "\n").Replace("\n", "<br>");
        return $"""
            <!doctype html><html><body style="margin:0;background:#f3f5f9;font-family:Arial,sans-serif;color:#172942">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td align="center" style="padding:28px 12px">
            <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="width:100%;max-width:600px;background:#ffffff;border-top:4px solid #ad6247">
            <tr><td style="padding:28px 28px 20px"><table role="presentation" cellpadding="0" cellspacing="0"><tr>
            <td valign="top">{LogoPlaceholder}</td><td valign="top">
            <h1 style="margin:0 0 8px;font-size:22px">{Encode(revision.CompanyNameSnapshot)}</h1>
            <p style="margin:0;color:#52637a;font-size:13px">{Encode(revision.CompanyEmailSnapshot)} · {Encode(revision.CompanyPhoneSnapshot)}</p>
            </td></tr></table></td></tr>
            <tr><td style="padding:20px 28px;background:#f3f5f9;border-top:1px solid #e5eaf0;border-bottom:1px solid #e5eaf0">
            <p style="margin:0 0 8px;font-size:12px;color:#52637a">QUOTATION · REVISION {revision.RevisionNumber}</p>
            <h2 style="margin:0 0 8px;font-size:20px">{Encode(document.Number)}</h2>
            <p style="margin:0;font-size:16px">{Encode(revision.Title)}</p>
            <p style="margin:8px 0 0;font-size:13px;color:#52637a">Prepared for {Encode(revision.CustomerNameSnapshot)}</p></td></tr>
            <tr><td style="padding:28px;font-size:15px;line-height:1.7">{content}
            <p style="margin:24px 0 0;padding-top:18px;border-top:1px solid #e5eaf0;font-size:13px;color:#52637a">Your quotation is attached as a PDF, including the scope of work, amounts and terms.</p></td></tr>
            </table></td></tr></table></body></html>
            """;
    }
}
