namespace FarmAppAspire.CustomerService.Models;

public enum BoxCategory { Insulated, FedEx }

public enum InsulatedBoxSize { FiveLb = 5, TenLb = 10, TwelveLb = 12 }

public enum FedExTierSize
{
    OneOz = 1,
    TwoOz = 2,
    FourOz = 4,
    EightOz = 8,
    OneLb = 16,
    TwoLb = 32,
    ThreeLb = 48,
    FiveLb = 80
}

public enum OrderFrequency { Weekly, BiWeekly, Monthly, OnRequest, Stopped }

public enum MonthlyWeek { First = 1, Second = 2, Third = 3, Fourth = 4 }

public enum StandingOrderStatus { Active, Paused, Stopped }

public enum OrderInstanceStatus { Pending, Harvested, Inspected, Shipped, Cancelled }

public enum OrderChannel { Insulated, FedEx }

public enum FedExPackagingType { Envelope, Box }
