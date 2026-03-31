using System;

namespace LegacyRenewalApp.Pricing;

public interface IDiscountRule
    {
        (decimal amount, string note) Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints);
    }

    public class SegmentDiscountRule : IDiscountRule
    {
        public (decimal amount, string note) Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints) => customer.Segment switch
        {
            "Silver" => (baseAmount * 0.05m, "silver discount; "),
            "Gold" => (baseAmount * 0.10m, "gold discount; "),
            "Platinum" => (baseAmount * 0.15m, "platinum discount; "),
            "Education" when plan.IsEducationEligible => (baseAmount * 0.20m, "education discount; "),
            _ => (0m, string.Empty)
        };
    }

    public class LoyaltyDiscountRule : IDiscountRule
    {
        public (decimal amount, string note) Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (customer.YearsWithCompany >= 5) return (baseAmount * 0.07m, "long-term loyalty discount; ");
            if (customer.YearsWithCompany >= 2) return (baseAmount * 0.03m, "basic loyalty discount; ");
            return (0m, string.Empty);
        }
    }

    public class TeamSizeDiscountRule : IDiscountRule
    {
        public (decimal amount, string note) Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (seatCount >= 50) return (baseAmount * 0.12m, "large team discount; ");
            if (seatCount >= 20) return (baseAmount * 0.08m, "medium team discount; ");
            if (seatCount >= 10) return (baseAmount * 0.04m, "small team discount; ");
            return (0m, string.Empty);
        }
    }

    public class PointsDiscountRule : IDiscountRule
    {
        public (decimal amount, string note) Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0) return (0m, string.Empty);
            int pointsToUse = Math.Min(customer.LoyaltyPoints, 200);
            return (pointsToUse, $"loyalty points used: {pointsToUse}; ");
        }
    }

    public interface IPaymentFeeStrategy
    {
        bool AppliesTo(string method);
        (decimal fee, string note) Calculate(decimal amount);
    }

    public class PaymentStrategies : IPaymentFeeStrategy
    {
        private readonly string _method;
        private readonly decimal _rate;
        private readonly string _note;

        public PaymentStrategies(string method, decimal rate, string note) 
        { _method = method; _rate = rate; _note = note; }

        public bool AppliesTo(string method) => method == _method;
        public (decimal fee, string note) Calculate(decimal amount) => (amount * _rate, _note);
    }

    public class RegionalTaxCalculator
    {
        public decimal GetTaxRate(string country) => country switch
        {
            "Poland" => 0.23m, "Germany" => 0.19m, "Czech Republic" => 0.21m, "Norway" => 0.25m, _ => 0.20m
        };
    }