# Subscription Renewal System (Refactored)

This project is a refactored version of a legacy subscription renewal system. The goal was to transform a monolithic "God Method" into a maintainable, testable, and decoupled architecture using **SOLID principles** and **Design Patterns**.

## Architectural Improvements

The refactoring addressed several key architectural issues by applying the following patterns:

### 1. Dependency Inversion & Decoupling
- **Interfaces**: Introduced `ICustomerRepository`, `ISubscriptionPlanRepository`, `IInvoicePublisher`, and `INotificationService` to remove direct dependencies on concrete implementations.
- **Adapter Pattern**: Wrapped the static `LegacyBillingGateway` within a `LegacyBillingAdapter`. This allows the business logic to interact with an abstraction rather than a hard-coded static class.

### 2. Open/Closed Principle (OCP)
- **Strategy Pattern**: Massive `if-else` blocks for discounts, payment fees, and regional taxes were replaced with independent strategy classes.
- **Extensibility**: Adding a new discount rule or payment method now only requires creating a new class that implements the relevant interface, rather than modifying the core service.

### 3. Single Responsibility & Cohesion
- **Pricing Engine**: All mathematical and business logic calculations were moved from the service into a dedicated `PricingEngine`.
- **Service Orchestration**: The `SubscriptionRenewalService` now acts strictly as an orchestrator, handling high-level flow rather than low-level calculations.

## Design Patterns Used
* **Strategy Pattern**: Used for polymorphic discount and fee calculations.
* **Adapter Pattern**: Used to wrap legacy static dependencies.
* **Dependency Injection**: Implemented via constructor injection (with a parameterless constructor preserved for backward compatibility).

## Constraints & Compatibility
- **Zero Breaking Changes**: The public contract of `CreateRenewalInvoice` remains identical to ensure `LegacyRenewalAppConsumer` continues to function without modification.
- **Legacy Integrity**: The `LegacyBillingGateway` class remains untouched as required.
- **Logic Accuracy**: All original business rules, including minimum subtotals ($300$) and minimum invoice amounts ($500$), have been preserved.

## Git Workflow
The project follows a clean, atomic commit history:
1.  **Infrastructure**: Extraction of interfaces and adapters.
2.  **Repositories**: Implementation of abstractions.
3.  **Domain**: Strategy-based pricing rules.
4.  **Integration**: Final service refactor and Dependency Injection setup.