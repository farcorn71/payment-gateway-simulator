# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.




# Key Design Considerations & Assumptions

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Domain-Driven Design](#domain-driven-design)
3. [Minimal APIs](#minimal-apis)
4. [Clean Architecture Principles](#clean-architecture-principles)
5. [Validation Strategy](#validation-strategy)
6. [Resilience & Retry Logic](#resilience--retry-logic)
7. [In-Memory Storage](#in-memory-storage)
8. [Security & Card Masking](#security--card-masking)
9. [Error Handling Philosophy](#error-handling-philosophy)
10. [Testing Approach](#testing-approach)
11. [Assumptions Made](#assumptions-made)
12. [How to Run the Project](#how-to-run-the-project)

---

## Architecture Overview

### Vertical Slice Architecture with CQRS-lite

This solution uses **Vertical Slice Architecture** rather than traditional layered architecture. Each feature (use case) is organized vertically from API → Handler → Domain → Infrastructure.

```
src/
├── PaymentGateway.Domain/           # Business logic & rules
├── PaymentGateway.Application/      # Use cases (Commands & Queries)
├── PaymentGateway.Infrastructure/   # External dependencies
└── PaymentGateway.Api/              # HTTP endpoints
```

**Why Vertical Slices?**
- ✅ Feature-focused organization
- ✅ Easy to locate all code for a specific feature
- ✅ Reduced coupling between features
- ✅ Simpler than full Clean Architecture for this scope

**Structure:**
```
Application/
└── Payments/
    ├── ProcessPayment/
    │   ├── ProcessPaymentCommand.cs
    │   ├── ProcessPaymentHandler.cs
    │   
    └── GetPayment/
        ├── GetPaymentQuery.cs
        ├── GetPaymentHandler.cs
        
```

---

## Domain-Driven Design

### Rich Domain Model with Value Objects

**Core Principle:** Make invalid states unrepresentable.

#### Value Objects Implemented

**1. CardNumber**
- Encapsulates card validation logic
- Luhn algorithm checksum validation
- Automatic masking (only last 4 digits exposed)
- Cannot create invalid card numbers

```csharp
var result = CardNumber.Create("4532015112830366");
// ✅ Validates: length, format, Luhn checksum
// ✅ Automatically masks: "************0366"
// ✅ Returns Result<CardNumber> (fail-safe)
```

**2. Money**
- Amount + Currency combined (cohesive concept)
- Amount stored as integer (minor units/cents)
- Prevents negative amounts
- Type-safe arithmetic operations

```csharp
var money = Money.Create(1000, Currency.Create("USD").Value);
// Represents $10.00
// 1000 = 1000 cents = $10.00
```

**3. PaymentId**
- Strongly-typed identifier
- Prevents passing wrong GUID to wrong method
- Compile-time type safety

```csharp
// ❌ Compile error: can't pass CustomerId where PaymentId expected
ProcessPayment(customerId);
```

**4. ExpiryDate**
- Month/Year validation
- Future date validation
- Domain-specific logic (not DateTime)

**5. Currency**
- ISO 3-letter code validation
- Supported: USD, GBP, EUR
- Prevents invalid currencies

**6. Cvv**
- 3-4 digit validation
- Numeric-only enforcement
- Never logged or stored

### Benefits of Value Objects

| Aspect | With Value Objects |
|--------|-------------------|
| **Validation** | Centralized, once at creation |
| **Type Safety** | Compile-time enforcement |
| **Reusability** | Used everywhere consistently |
| **Testing** | Test once in isolation |
| **Maintainability** | Change once, works everywhere |

---

## Minimal APIs

### Modern .NET Approach

**Choice:** Minimal APIs instead of Controllers for .NET 8+ projects.

#### Example Implementation

```csharp
// Minimal API (Our approach)
app.MapPost("/api/v1/payments", async (
    [FromBody] ProcessPaymentRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var command = new ProcessPaymentCommand(...);
    var result = await sender.Send(command, cancellationToken);
    
    return result.Match(
        onSuccess: response => Results.Ok(response),
        onFailure: error => MapErrorToHttp(error)
    );
});
```

#### Why Minimal APIs?

**Advantages:**
1. **Less Boilerplate** - No controller classes, base classes, attributes
2. **Explicit Dependencies** - Parameters clearly show what's needed
3. **Better Performance** - Less overhead than MVC controllers
4. **Easier Testing** - Direct function testing without mocking ControllerContext
5. **Modern .NET Pattern** - Microsoft's recommended approach

**Comparison:**

```csharp
// ❌ Traditional Controller (more ceremony)
[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    
    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }
    
    [HttpPost]
    public async Task<IActionResult> ProcessPayment(...)
    {
        // Same logic but more code
    }
}

// ✅ Minimal API (concise)
app.MapPost("/api/v1/payments", async (request, sender, ct) => 
{
    // Direct logic
});
```

#### Endpoint Organization

Endpoints are organized in static classes:

```csharp
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments");
            
        group.MapPost("", ProcessPayment);
        group.MapGet("{id}", GetPayment);
        
        return app;
    }
}
```

---

## Clean Architecture Principles

### Dependency Inversion

**Rule:** Dependencies point inward (toward domain).

```
┌─────────────────────────────────┐
│     API Layer (Endpoints)       │
│  Depends on: Application        │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│   Application Layer (Handlers)  │
│  Depends on: Domain, Interfaces │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│   Domain Layer (Business Logic) │
│  Depends on: Nothing!           │
└─────────────────────────────────┘
             ▲
             │
┌────────────┴────────────────────┐
│  Infrastructure (Implementations)│
│  Depends on: Interfaces         │
└─────────────────────────────────┘
```

### Interface Segregation

**Interfaces defined in Application layer, implemented in Infrastructure:**

```csharp
// Application/Common/Interfaces/IPaymentRepository.cs
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}

// Infrastructure/Repositories/InMemoryPaymentRepository.cs
public class InMemoryPaymentRepository : IPaymentRepository
{
    // Implementation details
}
```

**Benefits:**
- ✅ Easy to test (mock interfaces)
- ✅ Easy to swap implementations
- ✅ Domain doesn't depend on infrastructure

### MediatR for Decoupling

**Request/Response pattern with MediatR:**

```csharp
// API doesn't know about handlers
ISender sender;
var result = await sender.Send(command, cancellationToken);

// Handler processes the command
public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, Result<ProcessPaymentResponse>>
{
    public async Task<Result<ProcessPaymentResponse>> Handle(...)
    {
        // Business logic
    }
}
```

**Benefits:**
- ✅ API layer doesn't depend on handler implementations
- ✅ Cross-cutting concerns via pipeline behaviors
- ✅ Easy to add logging, validation, caching

---

## Validation Strategy

### Domain-Level Validation

**Principle:** Validate at the boundary where objects are created.

#### Fail-Fast Approach

```csharp
// Sequential validation with early return
var cardNumberResult = CardNumber.Create(request.CardNumber);
if (!cardNumberResult.IsSuccess)
{
    _logger.LogWarning("Payment {PaymentId} rejected: {Error}", 
        paymentId, cardNumberResult.Error!.Message);
    return Result<ProcessPaymentResponse>.Failure(cardNumberResult.Error!);
}

var expiryDateResult = ExpiryDate.Create(request.ExpiryMonth, request.ExpiryYear);
if (!expiryDateResult.IsSuccess)
{
    _logger.LogWarning("Payment {PaymentId} rejected: {Error}", 
        paymentId, expiryDateResult.Error!.Message);
    return Result<ProcessPaymentResponse>.Failure(expiryDateResult.Error!);
}

// Continue for remaining fields...
```

#### Validation Rules Summary

| Field | Validation Rules |
|-------|------------------|
| **Card Number** | Length: 14-19 digits<br>Format: Digits only<br>Checksum: Luhn algorithm |
| **Expiry Date** | Month: 1-12<br>Year: Current or future<br>Combined: Not expired |
| **Currency** | Format: 3 letters<br>Supported: USD, GBP, EUR |
| **Amount** | Positive (> 0)<br>Maximum: 9,999,999<br>Format: Integer (minor units) |
| **CVV** | Length: 3-4 digits<br>Format: Digits only |

#### Why Not FluentValidation?

**We chose domain validation over FluentValidation because:**

```csharp
// ❌ FluentValidation (external framework)
public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .Length(14, 19)
            .Must(BeValidLuhn);
    }
}

// ✅ Domain Validation (self-contained)
public static Result<CardNumber> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result<CardNumber>.Failure(Error.Validation(...));
        
    if (!IsValidLuhn(value))
        return Result<CardNumber>.Failure(Error.Validation(...));
        
    return Result<CardNumber>.Success(new CardNumber(value));
}
```

**Advantages of Domain Validation:**
- Validation IS business logic, belongs in domain
- Can't create invalid objects (compile-time safety)
- No external dependencies
- Easier to test in isolation
- Type-safe Result<T> return

---

## Resilience & Retry Logic

### Polly for Bank Communication

**Problem:** Bank simulator returns `503 Service Unavailable` for transient failures.

**Solution:** Exponential backoff retry policy.

```csharp
_retryPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => 
        r.StatusCode == HttpStatusCode.ServiceUnavailable)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => 
            TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            _logger.LogWarning(
                "Bank service unavailable. Retry {RetryCount} after {Delay}ms",
                retryCount, timespan.TotalMilliseconds);
        });
```

#### Retry Schedule

| Attempt | Delay | Calculation |
|---------|-------|-------------|
| 1st | Immediate | - |
| 2nd | 200ms | 2^1 × 100 = 200ms |
| 3rd | 400ms | 2^2 × 100 = 400ms |
| 4th | 800ms | 2^3 × 100 = 800ms |
| After | Fail | Return error to client |

#### Why Exponential Backoff?

**Benefits:**
1. **Prevents Thundering Herd** - Not all clients retry at once
2. **Gives Service Time to Recover** - Increasing delays allow recovery
3. **Industry Standard** - Used by AWS, Azure, Google Cloud

#### What Gets Retried?

```csharp
✅ Retry on:
- 503 Service Unavailable

❌ Don't retry on:
- 400 Bad Request (validation error, won't succeed on retry)
- 200 OK (success, no retry needed)
- Other 5xx errors (configuration issue, retry won't help)
```

#### Production Considerations

**Current implementation handles:**
- ✅ Transient failures (503)
- ✅ Exponential backoff
- ✅ Configurable retry count

**Production would add:**
- 🔒 Circuit breaker (stop retrying if service is down)
- 🔒 Bulkhead isolation (limit concurrent requests)
- 🔒 Timeout policies (don't wait forever)
- 🔒 Fallback strategies (use cached data, or distributed caching or even database)

---

## In-Memory Storage

### ConcurrentDictionary Implementation

**Why In-Memory?**

**Requirements explicitly state:**
> "You do not need to integrate with a real storage engine or database"

**Implementation:**

```csharp
public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken)
    {
        _payments.TryGetValue(id.Value, out var payment);
        return Task.FromResult(payment);
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        _payments.TryAdd(payment.Id.Value, payment);
        return Task.CompletedTask;
    }
}
```

---
**Why Luhn?**
- ✅ Catches typos (90% of errors)
- ✅ Detects digit transpositions
- ✅ Industry standard since 1954
- ✅ Required by PCI DSS

### Security Best Practices Applied

| Practice | Implementation | Status |
|----------|----------------|--------|
| **Card Masking** | Only last 4 digits returned | ✅ Implemented |
| **CVV Protection** | Never stored/logged | ✅ Implemented |
| **Input Validation** | All inputs validated | ✅ Implemented |
| **Luhn Validation** | Card checksum verification | ✅ Implemented |
| **HTTPS/TLS** | Encryption in transit | 🔒 Production requirement |
| **Tokenization** | Replace PAN with token | 🔒 Production requirement |
| **Key Management** | Secure secret storage | 🔒 Production requirement |
| **Audit Logging** | Track all access | 🔒 Production requirement |

---

## Error Handling Philosophy

### Result Pattern Over Exceptions

**Principle:** Use return values for expected failures, exceptions for unexpected failures.

```csharp
public  class Result<T> where T : class
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, null, error);
    
    public TOut Match<TOut>(
        Func<T, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}
```

#### Why Result Pattern?

**Traditional Exception-Based:**
```csharp
// ❌ Exceptions for business logic
public Payment ProcessPayment(PaymentRequest request)
{
    if (string.IsNullOrEmpty(request.CardNumber))
        throw new ValidationException("Card number required");
        
    if (!IsValidLuhn(request.CardNumber))
        throw new ValidationException("Invalid card number");
        
    // ... more validations, each throwing
}

// Caller
try
{
    var payment = ProcessPayment(request);
    return Ok(payment);
}
catch (ValidationException ex)
{
    return BadRequest(ex.Message);
}
catch (BankException ex)
{
    return StatusCode(502, ex.Message);
}
```

**Our Result Pattern:**
```csharp
// ✅ Result for business logic
public Result<Payment> ProcessPayment(PaymentRequest request)
{
    var cardResult = CardNumber.Create(request.CardNumber);
    if (!cardResult.IsSuccess)
        return Result<Payment>.Failure(cardResult.Error);
        
    // ... more validations, returning Result
        
    return Result<Payment>.Success(payment);
}

// Caller
var result = ProcessPayment(request);
return result.Match(
    onSuccess: payment => Ok(payment),
    onFailure: error => error.Type switch
    {
        ErrorType.Validation => BadRequest(error),
        ErrorType.External => StatusCode(502, error),
        _ => StatusCode(500, error)
    });
```

#### Benefits of Result Pattern

| Aspect | Result Pattern | Exception-Based |
|--------|----------------|-----------------|
| **Performance** | ✅ No stack unwinding | ❌ Expensive |
| **Explicit** | ✅ Signature shows failure | ❌ Hidden in docs |
| **Type-Safe** | ✅ Compiler enforces handling | ❌ Runtime only |
| **Control Flow** | ✅ Explicit, readable | ❌ Hidden jumps |
| **Testability** | ✅ Easy to test both paths | ⚠️ Need try-catch |

#### Error Classification

```csharp
public enum ErrorType
{
    Validation,  // 400 Bad Request - Client error
    NotFound,    // 404 Not Found - Resource doesn't exist
    Conflict,    // 409 Conflict - State conflict
    External     // 502 Bad Gateway - External service error
}

public record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string code, string message) => 
        new(ErrorType.Validation, code, message);
        
    public static Error NotFound(string code, string message) => 
        new(ErrorType.NotFound, code, message);
        
    public static Error External(string code, string message) => 
        new(ErrorType.External, code, message);
}
```

#### RFC 7807 Problem Details

**Consistent error responses:**

```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "Card number is invalid",
  "errorCode": "card_number.invalid_checksum"
}
```

---

## Testing Approach

### Test Pyramid

```
        /\
       /UI\ (0 tests - out of scope)
      /────\
     /  API \ (5 integration tests)
    /────────\
   /   Unit   \ (20+ unit tests)
  /────────────\
```

### Test Coverage

**Domain Tests (Unit):**
- `CardNumberTests.cs` - Luhn validation, masking
- `ExpiryDateTests.cs` - Date validation logic
- Other value objects

**Application Tests (Unit with Mocks):**
- `ProcessPaymentHandlerTests.cs` - 8 tests
  - Authorized payment flow
  - Declined payment flow
  - All validation scenarios
  - Bank failure scenarios

**API Tests (Integration):**
- `ProcessPaymentTests.cs` - End-to-end flows
  - Valid payment returns 200
  - Invalid inputs return 400
  - Bank errors return 502

### Why This Distribution?

1. **Most tests at Unit level** - Fast, isolated, catch bugs early
2. **Some Integration tests** - Verify real HTTP behavior
3. **No UI tests** - Out of scope for API project

---

## Assumptions Made

### Business Assumptions

1. **Amount in Minor Units**
   - All amounts are integers representing cents
   - $10.00 = 1000 (minor units)
   - No fractional cents exist

2. **Supported Currencies**
   - Only USD, GBP, EUR are supported
   - All use 100 minor units = 1 major unit
   - No JPY (no decimals) or KWD (3 decimals)

3. **Card Validation**
   - Luhn algorithm is sufficient
   - No BIN (Bank Identification Number) validation
   - No card brand detection required

4. **Payment States**
   - Only Authorized and Declined states
   - No Pending, Processing, Refunded states
   - No payment modification after creation

5. **Idempotency**
   - Not required for this implementation
   - Production would need idempotency keys

### Technical Assumptions

1. **Single Instance Deployment**
   - In-memory storage assumes single instance
   - No distributed caching needed
   - No session affinity required

2. **Synchronous Processing**
   - All payment processing is synchronous
   - No background jobs or queues
   - Immediate response to client

3. **No Authentication**
   - No API keys or JWT tokens
   - Open API for assessment purposes
   - Production would require auth

4. **No Rate Limiting**
   - No throttling or quotas
   - Assumes trusted environment
   - Production would add rate limiting

5. **No Audit Trail**
   - Only current state stored
   - No event history or audit log
   - Production would track all changes

### Infrastructure Assumptions

1. **Docker Environment**
   - Bank simulator runs in Docker
   - API can run locally or in Docker
   - Docker Compose for orchestration

2. **Network Reliability**
   - Retry logic handles transient failures
   - Assumes eventual connectivity
   - No offline mode or queueing

3. **No Monitoring**
   - Health checks available
   - Structured logging implemented
   - Production would add metrics, tracing

---

## How to Run the Project

### Prerequisites

- **.NET 8 SDK** installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Docker Desktop** installed and running
- **Git** for cloning (optional)

### Option 1: Docker Compose (Recommended)

**Fastest way to run everything:**

```bash
# 1. Navigate to project root
cd PaymentGateway

# 2. Start all services
docker-compose up -d

# 3. Verify services are running
docker-compose ps

# 4. Check health
curl http://localhost:5000/health

# 5. Open Swagger UI
# Browser: http://localhost:5000/swagger
```

**Services started:**
- Bank Simulator: `http://localhost:8080`
- Payment Gateway API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

**View logs:**
```bash
# All services
docker-compose logs -f

# API only
docker-compose logs -f payment-gateway-api
```

**Stop services:**
```bash
docker-compose down
```

### Option 2: Local Development

**Run API locally with bank simulator in Docker:**

```bash
# 1. Start bank simulator only
docker run -d -p 8080:8080 -p 2525:2525 \
  -v $(pwd)/imposters:/imposters \
  bbyars/mountebank --configfile /imposters/imposters.ejs --allowInjection

# 2. Navigate to API project
cd src/PaymentGateway.Api

# 3. Run the API
dotnet run

# API starts at: http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

### Option 3: Visual Studio / Rider

**Using an IDE:**

1. Open `PaymentGateway.sln`
2. Set `PaymentGateway.Api` as startup project
3. Ensure bank simulator is running (Docker)
4. Press **F5** or click **Run**
5. Browser opens to Swagger UI automatically

### Testing the API

**Using Swagger UI:**

1. Navigate to `http://localhost:5000/swagger`
2. Click `POST /api/v1/payments`
3. Click "Try it out"
4. Use this payload:

```json
{
  "cardNumber": "5425233430109903",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "USD",
  "amount": 1000,
  "cvv": "123"
}
```

5. Click "Execute"
6. Should return 200 OK with payment details

# Health check
curl http://localhost:5000/health
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test project
dotnet test tests/PaymentGateway.Domain.Tests/

# Run specific test
dotnet test --filter "FullyQualifiedName~CardNumberTests"
```

### Building the Solution

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Build specific configuration
dotnet build --configuration Release

# Clean build artifacts
dotnet clean
```

### Test Scenarios

**Authorized Payment (card ending in odd number):**
```json
{
  "cardNumber": "5425233430109903",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "USD",
  "amount": 1000,
  "cvv": "123"
}
```

**Declined Payment (card ending in even number):**
```json
{
  "cardNumber": "4532015112830366",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 500,
  "cvv": "456"
}
```

**Validation Error (expired card):**
```json
{
  "cardNumber": "5425233430109903",
  "expiryMonth": 1,
  "expiryYear": 2020,
  "currency": "USD",
  "amount": 1000,
  "cvv": "123"
}
```

### Troubleshooting

**Port already in use:**
```bash
# Change port in docker-compose.yml or use:
dotnet run --urls "http://localhost:5001"
```

**Bank simulator not responding:**
```bash
# Check if running
docker ps

# Restart
docker-compose restart bank-simulator

# View logs
docker-compose logs bank-simulator
```

**API can't connect to bank:**
Check `appsettings.json`:
```json
{
  "BankClient": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

### Verification Checklist

- [ ] Bank simulator running: `curl http://localhost:8080`
- [ ] API running: `curl http://localhost:5000/health`
- [ ] Swagger accessible: `http://localhost:5000/swagger`
- [ ] Can process payment successfully
- [ ] Can retrieve payment
- [ ] All tests pass: `dotnet test`
