namespace FarmAppAspire.CustomerService.Models;

// ── Product catalog DTOs ─────────────────────────────────────────────────────
public record InsulatedBoxConfigDto(InsulatedBoxSize Size, decimal WeightLbs, decimal BasePricePerLb, bool IsDefault);
public record FedExTierConfigDto(FedExTierSize TierSize, decimal WeightOz, decimal FixedPrice);

// ── Customer pricing DTOs ────────────────────────────────────────────────────
public record CustomerPricingDto(Guid Id, InsulatedBoxSize BoxSize, decimal PricePerLb, decimal? ShippingRate, int MinQty);
public record CreateCustomerPricingRequest(InsulatedBoxSize BoxSize, decimal PricePerLb, decimal? ShippingRate, int MinQty);
public record UpdateCustomerPricingRequest(decimal PricePerLb, decimal? ShippingRate, int MinQty);

// ── Standing order DTOs ───────────────────────────────────────────────────────
public record AllStandingOrderDto(
    Guid Id, Guid CustomerId, string CustomerDisplayName,
    ChannelType CustomerChannelType, CustomerType CustomerType,
    OrderChannel Channel,
    StandingOrderStatus Status, OrderFrequency Frequency, MonthlyWeek? MonthlyWeek,
    bool IsSample, int SeasonYear, DateTime StartWeek,
    int TotalBoxes, decimal TotalWeightLbs, decimal TotalAmount,
    DateTime CreatedAt, string CreatedBy);

public record StandingOrderLineDto(Guid Id, InsulatedBoxSize BoxSize, int Qty);
public record StandingOrderLineRequest(InsulatedBoxSize BoxSize, int Qty);
public record StandingOrderSkipDto(Guid Id, DateTime WeekOf);

public record StandingOrderDto(
    Guid Id, Guid CustomerId, Guid? ContactId,
    OrderChannel Channel,
    StandingOrderStatus Status, OrderFrequency Frequency, MonthlyWeek? MonthlyWeek,
    bool IsSample, int SeasonYear, DateTime StartWeek,
    IReadOnlyList<StandingOrderLineDto> Lines,
    IReadOnlyList<StandingOrderSkipDto> Skips,
    int TotalBoxes, decimal TotalWeightLbs, decimal TotalAmount,
    DateTime CreatedAt, string CreatedBy);

public record CreateStandingOrderRequest(
    OrderChannel Channel,
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

// ── Order DTOs ───────────────────────────────────────────────────────────────
public record OrderLineDto(
    Guid Id,
    InsulatedBoxSize? BoxSize,
    int Qty,
    decimal? EffectivePricePerLb,
    FedExTierSize? FedExTierSize,
    decimal? FedExFixedPrice,
    FedExPackagingType? PackagingType);

public record OrderDto(
    Guid Id, Guid? StandingOrderId, Guid CustomerId, Guid? ContactId,
    OrderChannel Channel, OrderStatus Status,
    DateTime WeekOf, DateTime? ShipDate, bool IsSample,
    IReadOnlyList<OrderLineDto> Lines,
    DateTime CreatedAt,
    int TotalQty,
    decimal TotalAmount);

public record InspectOrderRequest(DateTime InspectionDate);
public record UpdateOrderRequest(Guid? ContactId, bool IsSample);

// ── Order creation DTOs ──────────────────────────────────────────────────────
public record CreateOrderLineRequest(
    InsulatedBoxSize? BoxSize,
    FedExTierSize? FedExTierSize,
    int Qty);

public record CreateOrderRequest(
    OrderChannel Channel,
    DateTime WeekOf,
    IReadOnlyList<CreateOrderLineRequest> Lines,
    Guid? ContactId = null,
    bool IsSample = false);

// ── Invoice DTOs ──────────────────────────────────────────────────────────────
public record InvoiceDto(
    Guid Id, Guid OrderId, Guid CustomerId,
    OrderChannel Channel, int SeasonYear, int SeekNum,
    string Label, DateTime CreatedAt);

// ── Admin DTOs ────────────────────────────────────────────────────────────────
public record GenerateOrdersRequest(DateTime ForWeek);
public record CustomerKeyDto(string? CustomerKey, bool HasCollision);
public record SeasonRestartRequest(int TargetSeasonYear);

public record GeneratedOrderSummary(
    Guid OrderId,
    Guid CustomerId,
    string CustomerDisplayName,
    string? CustomerKey,
    string Channel,
    DateTime WeekOf,
    bool IsSample,
    int TotalQty,
    decimal TotalAmount);

public record GenerateOrdersResponse(
    int GeneratedCount,
    int SkippedCount,
    DateTime WeekOf,
    IReadOnlyList<GeneratedOrderSummary> Orders);

public record WeekOrderSummary(
    Guid OrderId,
    Guid CustomerId,
    string CustomerDisplayName,
    string? CustomerKey,
    string Channel,
    string Status,
    DateTime WeekOf,
    bool IsSample,
    int TotalQty,
    decimal TotalAmount);

public record AllOrdersSummaryDto(
    Guid Id,
    Guid? StandingOrderId,
    Guid CustomerId,
    string CustomerDisplayName,
    string CustomerChannelType,
    string CustomerType,
    string Channel,
    string Status,
    DateTime WeekOf,
    bool IsSample,
    int TotalQty,
    decimal TotalAmount);
