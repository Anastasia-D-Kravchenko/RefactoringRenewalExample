using LegacyRenewalApp.Interfaces;

namespace LegacyRenewalApp.Adapters;

public class LegacyBillingAdapter : IInvoicePublisher, INotificationService
{
    public void Save(RenewalInvoice invoice)
    {
        LegacyBillingGateway.SaveInvoice(invoice);
    }

    public void SendEmail(string email, string subject, string body)
    {
        LegacyBillingGateway.SendEmail(email, subject, body);
    }
}