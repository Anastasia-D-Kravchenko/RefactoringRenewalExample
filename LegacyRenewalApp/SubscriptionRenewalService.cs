using System;
using System.Collections.Generic;
using LegacyRenewalApp.Interfaces;
using LegacyRenewalApp.Adapters;
using LegacyRenewalApp.Pricing;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly PricingEngine _pricingEngine;
        private readonly IInvoicePublisher _invoicePublisher;
        private readonly INotificationService _notificationService;

        public SubscriptionRenewalService() : this(
            new CustomerRepository(),
            new SubscriptionPlanRepository(),
            CreateDefaultPricingEngine(),
            new LegacyBillingAdapter(),
            new LegacyBillingAdapter())
        {
        }

        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            PricingEngine pricingEngine,
            IInvoicePublisher invoicePublisher,
            INotificationService notificationService)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _pricingEngine = pricingEngine;
            _invoicePublisher = invoicePublisher;
            _notificationService = notificationService;
        }

        public RenewalInvoice CreateRenewalInvoice(int customerId, string planCode, int seatCount, string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints)
        {
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");

            string normPlan = planCode.Trim().ToUpperInvariant();
            string normPayment = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normPlan);

            if (!customer.IsActive) throw new InvalidOperationException("Inactive customers cannot renew subscriptions");

            var pricing = _pricingEngine.Calculate(customer, plan, seatCount, normPayment, includePremiumSupport, useLoyaltyPoints);

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normPlan}",
                CustomerName = customer.FullName,
                PlanCode = normPlan,
                PaymentMethod = normPayment,
                SeatCount = seatCount,
                BaseAmount = Math.Round(pricing.BaseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(pricing.DiscountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(pricing.SupportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(pricing.PaymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(pricing.TaxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(pricing.FinalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = pricing.Notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _invoicePublisher.Save(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                _notificationService.SendEmail(customer.Email, "Subscription renewal invoice", 
                    $"Hello {customer.FullName}, your renewal for plan {normPlan} has been prepared. Final amount: {invoice.FinalAmount:F2}.");
            }

            return invoice;
        }

        private static PricingEngine CreateDefaultPricingEngine()
        {
            var discountRules = new List<IDiscountRule> { new SegmentDiscountRule(), new LoyaltyDiscountRule(), new TeamSizeDiscountRule(), new PointsDiscountRule() };
            var paymentStrats = new List<IPaymentFeeStrategy> { 
                new PaymentStrategies("CARD", 0.02m, "card payment fee; "),
                new PaymentStrategies("BANK_TRANSFER", 0.01m, "bank transfer fee; "),
                new PaymentStrategies("PAYPAL", 0.035m, "paypal fee; "),
                new PaymentStrategies("INVOICE", 0m, "invoice payment; ")
            };
            return new PricingEngine(discountRules, paymentStrats, new RegionalTaxCalculator());
        }
    }
}