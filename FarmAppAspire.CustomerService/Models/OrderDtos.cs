namespace FarmAppAspire.CustomerService.Models;

// ── Product catalog DTOs ─────────────────────────────────────────────────────
public record InsulatedBoxConfigDto(InsulatedBoxSize Size, decimal WeightLbs, decimal BasePricePerLb, bool IsDefault);
public record FedExTierConfigDto(FedExTierSize TierSize, decimal WeightOz, decimal FixedPrice);

// ── Customer pricing DTOs ────────────────────────────────────────────────────
public record CustomerPricingDto(Guid Id, InsulatedBoxSize BoxSize, decimal PricePerLb, decimal? ShippingRate, int MinQty);
public record CreateCustomerPricingRequest(InsulatedBoxSize BoxSize, decimal PricePerLb, decimal? ShippingRate, int MinQty);
public record UpdateCustomerPricingRequest(decimal PricePerLb, decimal? ShippingRate, int MinQty);

// ── Standing order DTOs ───────────────────────────────────────────────────────
public record StandingOrderLineDto(Guid Id, InsulatedBoxSize BoxSize, int Qty);
public record StandingOrderLineRequest(InsulatedBoxSize BoxSize, int Qty);
public record StandingOrderSkipDto(Guid Id, DateTime WeekOf);

public record StandingOrderDto(
    Guid Id, Guid CustomerId, Guid? ContactId,
    StandingOrderStatus Status, OrderFrequency Frequency, MonthlyWeek? MonthlyWeek,
    bool IsSample, int SeasonYear, DateTime StartWeek,
    IReadOnlyList<StandingOrderLineDto> Lines,
    IReadOnlyList<StandingOrderSkipDto> Skips,
    int TotalBoxes, decimal TotalWeightLbs, decimal TotalAmount,
    DateTime CreatedAt, string CreatedBy);

public record CreateStandingOrderRequest(
    Guid? ContactId,
    OrderFrequency Frequency,
    MonthlyWeek? MonthlyWeek,
    bool IsSample,
    IReadOnlyList<StandingOrderLineRequest> Lines);

public record UpdateStandingOrderRequest(
    OrderFrequency Frequency,
    MonthlyWeek? MonthlyWeek,
    bool IsSample,
    Guid? ContactId,
    IReadOnlyList<StandingOrderLineRequest> Lines);

public record AddSkipWeekRequest(DateTime WeekOf);
public record UpdateStandingOrderContactRequest(Guid? ContactId);

// ── Order instance DTOs ───────────────────────────────────────────────────────
public record OrderInstanceLineDto(
    Guid Id,
    InsulatedBoxSize? BoxSize,
    int Qty,
    decimal? EffectivePricePerLb,
    FedExTierSize? FedExTierSize,
    decimal? FedExFixedPrice,
    FedExPackagingType? PackagingType);

public record OrderInstanceDto(
    Guid Id, Guid? StandingOrderId, Guid CustomerId, Guid? ContactId,
    OrderChannel Channel, OrderInstanceStatus Status,
    DateTime WeekOf, DateTime? ShipDate, bool IsSample,
    IReadOnlyList<OrderInstanceLineDto> Lines,
    DateTime CreatedAt);

public record InspectOrderRequest(DateTime InspectionDate);
public record UpdateOrderInstanceRequest(Guid? ContactId, bool IsSample);

// ── FedEx order DTOs ──────────────────────────────────────────────────────────
public record FedExOrderLineRequest(FedExTierSize TierSize, int Qty);
public record CreateFedExOrderRequest(Guid? ContactId, IReadOnlyList<FedExOrderLineRequest> Lines, DateTime? WeekOf = null, bool IsSample = false);

// ── Invoice DTOs ──────────────────────────────────────────────────────────────
public record InvoiceDto(
    Guid Id, Guid OrderInstanceId, Guid CustomerId,
    OrderChannel Channel, int SeasonYear, int SeekNum,
    string Label, DateTime CreatedAt);

// ── Admin DTOs ────────────────────────────────────────────────────────────────
public record GenerateInstancesRequest(DateTime ForWeek);
public record CustomerKeyDto(string? CustomerKey, bool HasCollision);
