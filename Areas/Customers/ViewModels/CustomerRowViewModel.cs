using BuildWise.Web.Services;

namespace BuildWise.Web.Areas.Customers.ViewModels;

public sealed record CustomerRowViewModel(CustomerEntry Customer, int RequestCount, int OpenRequests, DateOnly? LastRequest);
