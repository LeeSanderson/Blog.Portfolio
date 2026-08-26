# Good and Bad Tests

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```csharp
// GOOD: Tests observable behavior
[Fact]
public async Task UserCanCheckoutWithValidCart()
{
    var cart = CreateCart();
    cart.Add(product);
    var result = await Checkout(cart, paymentMethod);
    result.Status.Should().Be(CheckoutStatus.Confirmed);
}
```

Characteristics:

- Tests behavior users/callers care about
- Uses the public interface only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```csharp
// BAD: Tests implementation details
[Fact]
public async Task CheckoutCallsPaymentServiceProcess()
{
    var payment = Substitute.For<IPaymentService>();
    await Checkout(cart, paymentMethod, payment);
    await payment.Received(1).Process(cart.Total);
}
```

Red flags:

- Mocking internal collaborators
- Testing private methods
- Asserting on call counts/order
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT
- Verifying through external means instead of interface

```csharp
// BAD: Bypasses interface to verify
[Fact]
public async Task CreateUserSavesToDatabase()
{
    await CreateUser(new CreateUserRequest("Alice"));
    var row = await db.QuerySingleOrDefaultAsync(
        "SELECT * FROM Users WHERE Name = @Name", new { Name = "Alice" });
    row.Should().NotBeNull();
}

// GOOD: Verifies through interface
[Fact]
public async Task CreateUserMakesUserRetrievable()
{
    var user = await CreateUser(new CreateUserRequest("Alice"));
    var retrieved = await GetUser(user.Id);
    retrieved.Name.Should().Be("Alice");
}
```

**Tautological tests**: Expected value restates the implementation, so the test passes by construction.

```csharp
// BAD: Expected value is recomputed the way the code computes it
[Fact]
public void CalculateTotalSumsLineItems()
{
    var items = new[] { new LineItem(Price: 10), new LineItem(Price: 5) };
    var expected = items.Sum(i => i.Price);
    CalculateTotal(items).Should().Be(expected);
}

// GOOD: Expected value is an independent, known literal
[Fact]
public void CalculateTotalSumsLineItems()
{
    CalculateTotal([new LineItem(Price: 10), new LineItem(Price: 5)]).Should().Be(15);
}
```
