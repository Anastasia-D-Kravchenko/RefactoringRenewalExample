using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyRenewalApp.Pricing;

public class PricingResult
    {
        public decimal BaseAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal SupportFee { get; set; }
        public decimal PaymentFee { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class PricingEngine
    {
        private readonly IEnumerable<IDiscountRule> _discountRules;
        private readonly IEnumerable<IPaymentFeeStrategy> _paymentStrategies;
        private readonly RegionalTaxCalculator _taxCalculator;

        public PricingEngine(IEnumerable<IDiscountRule> discountRules, IEnumerable<IPaymentFeeStrategy> paymentStrategies, RegionalTaxCalculator taxCalculator)
        {
            _discountRules = discountRules;
            _paymentStrategies = paymentStrategies;
            _taxCalculator = taxCalculator;
        }

        public PricingResult Calculate(Customer customer, SubscriptionPlan plan, int seatCount, string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints)
        {
            var result = new PricingResult();
            
            result.BaseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;

            foreach (var rule in _discountRules)
            {
                var (amount, note) = rule.Calculate(customer, plan, seatCount, result.BaseAmount, useLoyaltyPoints);
                result.DiscountAmount += amount;
                result.Notes += note;
            }

            decimal subtotal = Math.Max(result.BaseAmount - result.DiscountAmount, 300m);
            if (subtotal == 300m && (result.BaseAmount - result.DiscountAmount) < 300m) result.Notes += "minimum discounted subtotal applied; ";

            if (includePremiumSupport)
            {
                result.SupportFee = plan.Code switch { "START" => 250m, "PRO" => 400m, "ENTERPRISE" => 700m, _ => 0m };
                result.Notes += "premium support included; ";
            }

            var paymentStrat = _paymentStrategies.FirstOrDefault(s => s.AppliesTo(paymentMethod)) 
                ?? throw new ArgumentException("Unsupported payment method");
            
            var feeData = paymentStrat.Calculate(subtotal + result.SupportFee);
            result.PaymentFee = feeData.fee;
            result.Notes += feeData.note;

            decimal taxBase = subtotal + result.SupportFee + result.PaymentFee;
            result.TaxAmount = taxBase * _taxCalculator.GetTaxRate(customer.Country);
            
            result.FinalAmount = Math.Max(taxBase + result.TaxAmount, 500m);
            if (result.FinalAmount == 500m && (taxBase + result.TaxAmount) < 500m) result.Notes += "minimum invoice amount applied; ";

            return result;
        }
    }