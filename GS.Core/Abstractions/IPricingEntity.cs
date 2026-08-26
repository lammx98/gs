namespace GS.Core.Abstractions;

public interface IPricingEntity
{
    int Quantity { get; set; }
    string? Unit { get; set; }
    decimal UnitPrice { get; set; }

    // Discount
    decimal? DiscountAmount { get; set; }
    decimal? DiscountPercentage { get; set; }

    // Tax
    decimal? TaxPercentage { get; set; }
    decimal? TaxAmount { get; set; }

    /// <summary>
    /// Quantity * UnitPrice - DiscountAmount
    /// </summary>
    decimal SubTotal { get; set; }
    /// <summary>
    /// SubTotal + TaxAmount
    /// </summary>
    decimal TotalAmount { get; set; }
}