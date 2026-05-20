using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Backend;

/// <summary>
/// Converts PascalCase controller names to kebab-case URL segments.
/// e.g. SalesInvoices → sales-invoices, PurchaseInvoices → purchase-invoices
/// Registered via RouteTokenTransformerConvention in Program.cs.
/// </summary>
public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value == null) return null;
        return Regex.Replace(value.ToString()!, "([a-z])([A-Z])", "$1-$2").ToLower();
    }
}
