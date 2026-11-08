using System.Data;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Quotations.ViewModels;
using BuildWise.Web.Services.Quotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Quotations.Pages;

public class UpsertModel(
    BuildWiseDbContext db,
    ICurrentUserService currentUser,
    GeminiQuotationWordingService wordingService,
    IConfiguration configuration,
    ILogger<UpsertModel> logger) : PageModel
{
    [BindProperty]
    public QuotationUpsertViewModel Input { get; set; } = new();

    public List<SelectListItem> Requests { get; private set; } = [];
    public List<SelectListItem> Currencies { get; private set; } = [];
    public bool CreatesRevision { get; private set; }
    public int RevisionNumber { get; private set; } = 1;
    public bool WordingConfigured { get; private set; }
    public bool ScopeItemsReviewed { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? requestId)
    {
        if (id.HasValue)
        {
            var quotation = await db.Quotations.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id);
            if (quotation is null) return NotFound();
            var revision = await LatestAsync(quotation.Id, tracking: false);
            Input.Id = quotation.Id;
            Input.RequestId = quotation.RequestId;
            if (revision is not null)
            {
                Input.RevisionId = revision.Id;
                Input.Version = VersionOf(revision);
                Input.Title = revision.Title;
                Input.IncludesPrices = revision.IncludesPrices;
                Input.CurrencyCode = revision.CurrencyCode;
                Input.TaxAmount = revision.TaxAmount ?? 0;
                Input.IssueDate = revision.IssueDate;
                Input.ValidUntil = revision.ValidUntil;
                Input.Terms = revision.Terms;
                Input.Exclusions = revision.Exclusions;
                Input.Items = revision.QuotationItems.OrderBy(item => item.Title).Select(item => new QuotationItemInputViewModel
                {
                    ScopeItemId = item.ScopeItemId, Title = item.Title, Description = item.Description,
                    Quantity = item.Quantity, LineAmount = item.LineAmount
                }).ToList();
                SetRevisionInfo(revision);
            }
            else
            {
                await FillFromRequestAsync(quotation.RequestId);
            }
        }
        else
        {
            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync();
            Input.CurrencyCode = company?.DefaultCurrency;
            Input.Terms = company?.DefaultQuotationTerms;
            Input.IssueDate = DateTime.UtcNow.Date;
            Input.ValidUntil = Input.IssueDate.Value.AddDays(company?.DefaultValidityDays ?? 14);
            if (requestId.HasValue && !await FillFromRequestAsync(requestId.Value)) return NotFound();
        }
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostLoadScopeAsync(Guid? id)
    {
        if (id.HasValue || Input.Id.HasValue) return BadRequest();
        if (!await FillFromRequestAsync(Input.RequestId))
            ModelState.AddModelError("Input.RequestId", "Select an open request with scope items.");
        else
            ModelState.Clear();
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostPrepareWordingAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id != Input.Id) return BadRequest();
        Input.Items ??= [];
        if (Input.Items.Count is < 1 or > 200)
            ModelState.AddModelError(string.Empty, "Load scope items before preparing the quotation wording.");

        var itemIds = Input.Items.Select(item => item.ScopeItemId).Distinct().ToList();
        if (itemIds.Count != Input.Items.Count)
            ModelState.AddModelError(string.Empty, "The quotation contains duplicate or missing scope items. Reload the scope before continuing.");

        var request = await db.ClientRequests.AsNoTracking()
            .Include(request => request.ScopeItems).ThenInclude(item => item.ScopeClauses)
            .Include(request => request.ScopeItems).ThenInclude(item => item.ScopeStages)
            .SingleOrDefaultAsync(request => request.Id == Input.RequestId
                && request.StatusId != (int)ClientRequestStatus.Closed
                && request.StatusId != (int)ClientRequestStatus.cancelled, cancellationToken);
        var reviewedItems = request?.ScopeItems
            .Where(item => itemIds.Contains(item.Id)
                && item.ReviewStatusId == (int)ScopeReviewStatus.Reviewed)
            .ToList() ?? [];
        if (request is null || reviewedItems.Count != itemIds.Count || itemIds.Count == 0)
            ModelState.AddModelError(string.Empty, "Review every included scope item before preparing quotation wording.");

        if (ModelState.IsValid)
        {
            try
            {
                var source = reviewedItems.Select(item => new QuotationScopeSource(
                    item.Id,
                    item.Title,
                    item.Description,
                    item.Quantity,
                    item.UnitCodeSnapshot,
                    item.ScopeClauses.Select(clause => $"{(ScopeClauseType)clause.ClauseTypeId}: {clause.Description}").ToList(),
                    item.ScopeStages.OrderBy(stage => stage.CreationDate).Select(stage => $"{stage.Name}: {stage.Description}").ToList()))
                    .ToList();
                var generated = await wordingService.GenerateAsync(source, cancellationToken);
                foreach (var (item, index) in Input.Items.Select((item, index) => (item, index)))
                {
                    item.Description = generated.ItemDescriptions[item.ScopeItemId];
                    ModelState.Remove($"Input.Items[{index}].Description");
                }
                Input.Exclusions = generated.AssumptionsAndExclusions;
                ModelState.Remove("Input.Exclusions");
                ViewData["QuotationWordingPrepared"] = true;
            }
            catch (QuotationWordingUnavailableException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Could not connect while preparing quotation wording for request {RequestId}.", Input.RequestId);
                ModelState.AddModelError(string.Empty, "Quotation wording is temporarily unavailable. Try again shortly.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Quotation wording preparation failed for request {RequestId}.", Input.RequestId);
                ModelState.AddModelError(string.Empty, "Quotation wording could not be prepared. Try again.");
            }
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (id != Input.Id) return BadRequest();
        var creatorId = await currentUser.GetUserIdAsync();
        if (!creatorId.HasValue) return Unauthorized();

        // Serialize saves so a stale form cannot overwrite a draft or create a duplicate revision number.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var quotation = id.HasValue ? await db.Quotations.SingleOrDefaultAsync(q => q.Id == id) : null;
        if (id.HasValue && quotation is null) return NotFound();
        var latest = quotation is null ? null : await LatestAsync(quotation.Id, tracking: true);
        SetRevisionInfo(latest);
        if (Input.RevisionId != latest?.Id || Input.Version != (latest is null ? null : VersionOf(latest)))
            ModelState.AddModelError(string.Empty, "This quotation has changed. Reload the page before saving.");
        if (quotation is not null && Input.RequestId != quotation.RequestId) return BadRequest();

        var request = await db.ClientRequests.Include(r => r.Client)
            .Include(r => r.ScopeItems).ThenInclude(item => item.ScopeStages)
            .Include(r => r.ScopeItems).ThenInclude(item => item.ScopeDependencyScopeItems)
            .SingleOrDefaultAsync(r => r.Id == Input.RequestId);
        if (request is null || request.StatusId == (int)ClientRequestStatus.Closed
            || request.StatusId == (int)ClientRequestStatus.cancelled || request.ScopeItems.Count == 0)
            ModelState.AddModelError("Input.RequestId", "Select an open request with scope items.");
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync();
        if (latest is null && company is null)
            ModelState.AddModelError(string.Empty, "Save the business profile before creating a quotation.");

        Input.Title = (Input.Title ?? string.Empty).Trim();
        Input.CurrencyCode = Input.CurrencyCode?.Trim().ToUpperInvariant();
        if (Input.Title.Length == 0) ModelState.AddModelError("Input.Title", "Enter a quotation title.");
        if (Input.ValidUntil.HasValue && Input.IssueDate.HasValue && Input.ValidUntil < Input.IssueDate)
            ModelState.AddModelError("Input.ValidUntil", "Validity must end on or after the issue date.");
        if (Input.IncludesPrices && !await db.Currencies.AnyAsync(c => c.Code == Input.CurrencyCode)
            && (latest?.CurrencyCode is null || latest.CurrencyCode != Input.CurrencyCode))
            ModelState.AddModelError("Input.CurrencyCode", "Select a configured currency.");
        Input.Items ??= [];
        if (Input.Items.Count is < 1 or > 200 || Input.Items.Select(i => i.ScopeItemId).Distinct().Count() != Input.Items.Count)
            ModelState.AddModelError(string.Empty, "Include between 1 and 200 distinct scope items.");
        for (var i = 0; i < Input.Items.Count; i++)
        {
            var item = Input.Items[i];
            item.Title = (item.Title ?? string.Empty).Trim();
            if (request is null || !request.ScopeItems.Any(scope => scope.Id == item.ScopeItemId))
                ModelState.AddModelError($"Input.Items[{i}].ScopeItemId", "The item must belong to the selected request.");
            var scope = request?.ScopeItems.FirstOrDefault(scope => scope.Id == item.ScopeItemId);
            var previous = latest?.QuotationItems.FirstOrDefault(previous => previous.ScopeItemId == item.ScopeItemId);
            var assessmentNotes = previous is null ? scope?.AssessmentNotesJson : previous.AssessmentNotesSnapshot;
            if (assessmentNotes?.Length > 50)
                ModelState.AddModelError(string.Empty, $"Assessment notes for '{item.Title}' exceed the quotation table's 50-character limit. Shorten the scope notes before saving.");
            if (item.Title.Length == 0) ModelState.AddModelError($"Input.Items[{i}].Title", "Enter an item title.");
            if (Input.IncludesPrices && !item.LineAmount.HasValue)
                ModelState.AddModelError($"Input.Items[{i}].LineAmount", "Enter the full line amount, including quantity.");
        }
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var now = DateTime.UtcNow;
        if (quotation is null)
        {
            var quotationId = Guid.NewGuid();
            quotation = new Quotation
            {
                Id = quotationId, RequestId = request!.Id, CreatorId = creatorId.Value,
                QuotationNumber = $"QT-{now:yyyyMMdd}-{quotationId:N}"
            };
            db.Quotations.Add(quotation);
        }
        var revision = latest;
        if (latest is null || latest.StatusId == (int)QuotationStatus.Approved)
        {
            revision = new QuotationRevision();
            if (latest is not null) db.Entry(revision).CurrentValues.SetValues(latest);
            revision.Id = Guid.NewGuid();
            revision.QuotationId = quotation.Id;
            revision.RevisionNumber = (latest?.RevisionNumber ?? 0) + 1;
            revision.CreatorId = creatorId.Value;
            revision.CreationDate = now;
            revision.ApprovedByUserId = null;
            revision.ApprovalDate = null;
            revision.ApprovedDocumentJson = null;
            db.QuotationRevisions.Add(revision);
            if (latest is null) SetSnapshots(revision, request!, company!);
        }
        else if (latest.StatusId != (int)QuotationStatus.Draft)
        {
            ModelState.AddModelError(string.Empty, "Only drafts and approved quotations can be revised.");
            await LoadOptionsAsync();
            return Page();
        }

        revision!.StatusId = (int)QuotationStatus.Draft;
        revision.Title = Input.Title;
        revision.Terms = Input.Terms?.Trim();
        revision.Exclusions = Input.Exclusions?.Trim();
        revision.IssueDate = Input.IssueDate;
        revision.ValidUntil = Input.ValidUntil;
        revision.IncludesPrices = Input.IncludesPrices;
        revision.CurrencyCode = Input.IncludesPrices ? Input.CurrencyCode : null;
        revision.Subtotal = Input.IncludesPrices ? Input.Items.Sum(i => Round(i.LineAmount!.Value)) : null;
        revision.TaxAmount = Input.IncludesPrices ? Round(Input.TaxAmount) : null;
        revision.Total = Input.IncludesPrices ? revision.Subtotal + revision.TaxAmount : null;
        revision.LastUpdate = now;
        var previousItems = latest?.QuotationItems.ToDictionary(item => item.ScopeItemId) ?? [];
        db.QuotationItems.RemoveRange(revision.QuotationItems);
        foreach (var input in Input.Items)
        {
            var scope = request!.ScopeItems.Single(item => item.Id == input.ScopeItemId);
            previousItems.TryGetValue(scope.Id, out var previous);
            db.QuotationItems.Add(new QuotationItem
            {
                Id = Guid.NewGuid(), QuotationRevisionId = revision.Id, RequestId = request.Id,
                ScopeItemId = scope.Id, ServiceId = previous?.ServiceId ?? scope.ServiceId,
                ServiceCodeSnapshot = previous?.ServiceCodeSnapshot ?? scope.ServiceCodeSnapshot,
                UnitCodeSnapshot = previous?.UnitCodeSnapshot ?? scope.UnitCodeSnapshot,
                Title = input.Title, Description = input.Description?.Trim(), Quantity = input.Quantity,
                LineAmount = Input.IncludesPrices ? Round(input.LineAmount!.Value) : null,
                RequiresSiteAssessment = previous?.RequiresSiteAssessment ?? scope.RequireAssessment,
                AssessmentNotesSnapshot = previous is null ? scope.AssessmentNotesJson : previous.AssessmentNotesSnapshot,
                WorkStagesSnapshotJson = previous is null
                    ? JsonSerializer.Serialize(scope.ScopeStages.Select(stage => new { stage.Name, stage.Description }))
                    : previous.WorkStagesSnapshotJson,
                DependenciesSnapshotJson = previous is null
                    ? JsonSerializer.Serialize(scope.ScopeDependencyScopeItems.Select(d => new { d.DependsOnScopeItemId, d.NotesJson }))
                    : previous.DependenciesSnapshotJson
            });
        }
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["SuccessMessage"] = $"Quotation {quotation.QuotationNumber}, revision {revision.RevisionNumber}, saved as draft.";
        return RedirectToPage("./Index");
    }

    private Task<QuotationRevision?> LatestAsync(Guid id, bool tracking)
    {
        var query = db.QuotationRevisions.Include(r => r.QuotationItems).Where(r => r.QuotationId == id);
        return (tracking ? query : query.AsNoTracking()).OrderByDescending(r => r.RevisionNumber).FirstOrDefaultAsync();
    }

    private static string VersionOf(QuotationRevision revision) => (revision.LastUpdate ?? revision.CreationDate).Ticks.ToString();
    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private void SetRevisionInfo(QuotationRevision? revision)
    {
        CreatesRevision = revision?.StatusId == (int)QuotationStatus.Approved;
        RevisionNumber = (revision?.RevisionNumber ?? 1) + (CreatesRevision ? 1 : 0);
    }

    private async Task<bool> FillFromRequestAsync(Guid id)
    {
        var request = await db.ClientRequests.AsNoTracking().Include(r => r.ScopeItems)
            .SingleOrDefaultAsync(r => r.Id == id && r.StatusId != (int)ClientRequestStatus.Closed
                && r.StatusId != (int)ClientRequestStatus.cancelled && r.ScopeItems.Any());
        if (request is null) return false;
        Input.RequestId = request.Id;
        Input.Title = request.Title;
        Input.Items = request.ScopeItems.OrderBy(item => item.Title).Select(item => new QuotationItemInputViewModel
        {
            ScopeItemId = item.Id, Title = item.Title, Description = item.Description, Quantity = item.Quantity
        }).ToList();
        return true;
    }

    private async Task LoadOptionsAsync()
    {
        WordingConfigured = !string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"])
            && !string.IsNullOrWhiteSpace(configuration["Gemini:Model"]);
        var itemIds = Input.Items.Select(item => item.ScopeItemId).Distinct().ToList();
        var reviewedCount = itemIds.Count == 0 ? 0 : await db.ScopeItems.AsNoTracking()
            .CountAsync(item => item.RequestId == Input.RequestId && itemIds.Contains(item.Id)
                && item.ReviewStatusId == (int)ScopeReviewStatus.Reviewed);
        ScopeItemsReviewed = itemIds.Count > 0 && reviewedCount == Input.Items.Count;
        Requests = await db.ClientRequests.AsNoTracking()
            .Where(r => (r.StatusId != (int)ClientRequestStatus.Closed && r.StatusId != (int)ClientRequestStatus.cancelled
                && r.ScopeItems.Any()) || r.Id == Input.RequestId)
            .OrderBy(r => r.Title).Select(r => new SelectListItem
            {
                Value = r.Id.ToString(), Text = (r.ReferenceNumber ?? "Request") + " · " + r.Client.Name + " · " + r.Title
            }).ToListAsync();
        Currencies = await db.Currencies.AsNoTracking().OrderBy(c => c.Code)
            .Select(c => new SelectListItem { Value = c.Code, Text = c.Code + " · " + c.Name }).ToListAsync();
        if (!string.IsNullOrWhiteSpace(Input.CurrencyCode) && !Currencies.Any(c => c.Value == Input.CurrencyCode))
            Currencies.Add(new SelectListItem { Value = Input.CurrencyCode, Text = Input.CurrencyCode });
    }

    private static void SetSnapshots(QuotationRevision revision, ClientRequest request, Company company)
    {
        revision.CompanyNameSnapshot = company.Name;
        revision.CompanyEmailSnapshot = company.Email;
        revision.CompanyPhoneSnapshot = company.Phone;
        revision.CompanyAddressSnapshot = company.Address;
        revision.CompanyRegistrationNumberSnapshot = company.RegistrationNumber;
        revision.CompanyTaxNumberSnapshot = company.TaxNumber;
        revision.CompanyLogoStorageKeySnapshot = company.LogoStorageKey;
        revision.CustomerNameSnapshot = request.Client.Name;
        revision.CustomerEmailSnapshot = request.Client.Email;
        revision.CustomerPhoneSnapshot = request.Client.Phone;
        revision.CustomerAddressSnapshot = request.Client.Address;
        revision.SiteAddressSnapshot = request.SiteAddress;
        revision.RequestDescriptionSnapshot = request.Description ?? string.Empty;
    }
}
