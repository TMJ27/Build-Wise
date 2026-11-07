using System.Text.Json;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Scopes.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.Scopes;

public static class ScopeQueries
{
    public static ScopeDependencyDetailsViewModel MapDependency(ScopeDependency dependency) => new()
    {
        Id = dependency.Id, ScopeItemId = dependency.ScopeItemId, RequestId = dependency.ScopeItem.RequestId,
        ScopeItemTitle = dependency.ScopeItem.Title, DependsOnScopeItemId = dependency.DependsOnScopeItemId,
        DependsOnTitle = dependency.DependsOnScopeItem.Title, Notes = ReadNotes(dependency.NotesJson),
        CreationDate = dependency.CreationDate,
        CreatorName = string.Join(" ", new[] { dependency.Creator.Name, dependency.Creator.Surname }
            .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } name ? name : dependency.Creator.Email,
        CanEdit = CanEdit(dependency.ScopeItem.Request.StatusId)
    };

    public static ScopeStageDetailsViewModel MapStage(ScopeStage stage) => new()
    {
        Id = stage.Id, ScopeItemId = stage.ScopeItemId, RequestId = stage.ScopeItem.RequestId,
        ScopeItemTitle = stage.ScopeItem.Title, Name = stage.Name, Description = stage.Description,
        CreationDate = stage.CreationDate,
        CreatorName = string.Join(" ", new[] { stage.Creator.Name, stage.Creator.Surname }
            .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } name ? name : stage.Creator.Email,
        CanEdit = CanEdit(stage.ScopeItem.Request.StatusId)
    };

    public static ScopeClauseDetailsViewModel MapClause(ScopeClause clause) => new()
    {
        Id = clause.Id, ScopeItemId = clause.ScopeItemId, RequestId = clause.ScopeItem.RequestId,
        ScopeItemTitle = clause.ScopeItem.Title, ClauseTypeId = clause.ClauseTypeId, Description = clause.Description,
        CreationDate = clause.CreationDate,
        CreatorName = string.Join(" ", new[] { clause.Creator.Name, clause.Creator.Surname }
            .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } name ? name : clause.Creator.Email,
        CanEdit = CanEdit(clause.ScopeItem.Request.StatusId)
    };

    public static IQueryable<ScopeViewModel> Requests(BuildWiseDbContext db) => db.ClientRequests.AsNoTracking()
        .Select(request => new ScopeViewModel
        {
            RequestId = request.Id, ReferenceNumber = request.ReferenceNumber, Title = request.Title,
            CustomerName = request.Client.Name, SiteAddress = request.SiteAddress, Description = request.Description,
            StatusId = request.StatusId, ItemCount = request.ScopeItems.Count,
            AssessmentCount = request.ScopeItems.Count(item => item.RequireAssessment)
        });

    public static bool CanEdit(int statusId) => statusId != (int)ClientRequestStatus.Closed
        && statusId != (int)ClientRequestStatus.cancelled;

    public static ScopeItemDetailsViewModel MapItem(ScopeItem item) => new()
    {
        Id = item.Id, RequestId = item.RequestId, ServiceId = item.ServiceId,
        Title = item.Title, Description = item.Description, ServiceName = item.Service.Name,
        ServiceCodeSnapshot = item.ServiceCodeSnapshot, Quantity = item.Quantity,
        UnitCodeSnapshot = item.UnitCodeSnapshot, RequireAssessment = item.RequireAssessment,
        AssessmentNotes = ReadNotes(item.AssessmentNotesJson),
        ReviewStatusName = Enum.IsDefined(typeof(ScopeReviewStatus), item.ReviewStatusId)
            ? ((ScopeReviewStatus)item.ReviewStatusId).ToString() : "Unknown",
        ReviewedBy = item.ReviewedOn.HasValue
            ? string.Join(" ", new[] { item.ReviewedByUser.Name, item.ReviewedByUser.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } name
                ? name : item.ReviewedByUser.Email
            : null,
        ReviewedOn = item.ReviewedOn,
        CanEdit = CanEdit(item.Request.StatusId)
    };

    public static string? ReadNotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String) return root.GetString();
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("notes", out var notes)
                && notes.ValueKind == JsonValueKind.String) return notes.GetString();
        }
        catch (JsonException) { }
        // Retain legacy text and other JSON shapes without losing their contents on edits.
        return json;
    }
}
