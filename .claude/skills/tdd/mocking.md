# When to Mock

Mock at **system boundaries** only:

- External APIs (payment, email, etc.)
- Databases (sometimes - prefer test DB)
- Time/randomness
- File system (sometimes)

Don't mock:

- Your own classes/modules
- Internal collaborators
- Anything you control

**"Anything you control" means the behaviour, not the interface.** An interface you declared is still a system
boundary if what sits behind it is not yours. In this repo `ISubscriberStore` is a port over Azure Table
Storage, `IEmailOutbox` over Azure Queue Storage, and `IEmailSender` over Resend — all owned interfaces, none
of them internal collaborators, and all legitimately substituted in tests. The question to ask is not "did I
write this interface?" but "does a real call to it leave the process?" If it does, it belongs to the first
list, however local the type looks.

## Designing for Mockability

At system boundaries, design interfaces that are easy to mock:

**1. Use dependency injection**

Pass external dependencies in via the constructor rather than creating them internally:

```csharp
// Easy to mock
public class PaymentProcessor(IPaymentClient paymentClient)
{
    public Task<PaymentResult> ProcessPayment(Order order) =>
        paymentClient.Charge(order.Total);
}

// Hard to mock
public class PaymentProcessor
{
    public Task<PaymentResult> ProcessPayment(Order order)
    {
        var client = new StripeClient(Environment.GetEnvironmentVariable("STRIPE_KEY"));
        return client.Charge(order.Total);
    }
}
```

**2. Prefer SDK-style interfaces over generic fetchers**

Declare a specific method for each external operation instead of one generic method with conditional logic:

```csharp
// GOOD: Each member is independently mockable
public interface IOrdersApi
{
    Task<User> GetUser(string id);
    Task<IReadOnlyList<Order>> GetOrders(string userId);
    Task<Order> CreateOrder(CreateOrderRequest request);
}

// BAD: Mocking requires conditional logic inside the mock
public interface IOrdersApi
{
    Task<HttpResponseMessage> Send(string endpoint, HttpMethod method, object? body);
}
```

The SDK approach means:
- Each substituted `IOrdersApi` member returns one specific shape
- No conditional logic in test setup
- Easier to see which operations a test exercises
- Compiler-checked types per operation
