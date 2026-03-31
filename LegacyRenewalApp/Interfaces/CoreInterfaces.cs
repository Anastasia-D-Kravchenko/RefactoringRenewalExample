namespace LegacyRenewalApp.Interfaces;

public interface ICustomerRepository
{
    Customer GetById(int customerId);
}

public interface ISubscriptionPlanRepository
{
    SubscriptionPlan GetByCode(string code);
}

public interface IInvoicePublisher
{
    void Save(RenewalInvoice invoice);
}

public interface INotificationService
{
    void SendEmail(string email, string subject, string body);
}