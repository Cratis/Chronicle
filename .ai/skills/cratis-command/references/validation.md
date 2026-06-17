# Command Validation — Reference

Validation in Cratis Arc runs in two places: **client-side** (via the proxy, before the request is sent) and **server-side** (the full pipeline, always).

---

## FluentValidation (recommended)

```csharp
// Must extend CommandValidator<T>, not AbstractValidator<T>
public class CreateOrderValidator : CommandValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty().WithMessage("Customer is required");
        RuleFor(c => c.Total).GreaterThan(0).WithMessage("Total must be positive");
        RuleFor(c => c.Items).NotEmpty().WithMessage("Order must have at least one item");
    }
}
```

**Why `CommandValidator<T>`?** It marks the class for automatic discovery (no DI registration needed) and allows the proxy generator to extract the rules into TypeScript for client-side pre-flight.

Rules that can be extracted and run client-side:
- `NotEmpty`, `NotNull`
- `MaximumLength`, `MinimumLength`, `Length`
- `GreaterThan`, `LessThan`, `GreaterThanOrEqualTo`, `LessThanOrEqualTo`
- `Must` with simple predicates
- `EmailAddress`

Rules that only run server-side (cannot be extracted):
- Validators with injected dependencies (e.g. database uniqueness checks)

---

## Data Annotations

```csharp
public record CreateOrder(
    [Required] Guid CustomerId,
    [Range(0.01, double.MaxValue, ErrorMessage = "Total must be positive")] decimal Total,
    [Required, MinLength(1)] List<OrderItem> Items
);
```

Simpler but less flexible than FluentValidation. Rules are enforced server-side and reflected in `CommandResult.validationResults`.

---

## Automatic validate endpoint

For every `[HttpPost]` command, Arc registers a parallel endpoint:

- Execute: `POST /api/orders/create`
- Validate: `POST /api/orders/create/validate`

The validate endpoint runs all authorization and validation filters but **never** calls the handler. No side effects.

---

## Client-side validate() in React

```tsx
const [command] = CreateOrder.use();
const [errors, setErrors] = useState<Record<string, string>>({});

// Option A: validate on blur
const handleBlur = async (field: string) => {
    const result = await command.validate();
    const fieldError = result.validationResults.find(v => v.propertyName === field);
    setErrors(prev => ({ ...prev, [field]: fieldError?.message ?? '' }));
};

// Option B: validate proactively as user types
useEffect(() => {
    command.validate().then(result => {
        setCanSubmit(result.isSuccess);
    });
}, [command.hasChanges]);

// Option C: validate + conditional execute
const handleSubmit = async () => {
    const validation = await command.validate();
    if (!validation.isValid) {
        setErrors(validation.validationResults.reduce(...));
        return;
    }
    await command.execute();
};
```

---

## Showing validation errors per field

```tsx
const getError = (field: keyof typeof command) =>
    result.validationResults.find(v => v.propertyName === String(field))?.message;

<input value={command.name} onChange={...} />
{getError('name') && <span className="error">{getError('name')}</span>}
```

`propertyName` in the result is camelCase matching the C# property name (lowercased first letter).
