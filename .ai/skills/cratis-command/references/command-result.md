# CommandResult — Reference

Arc wraps every command response in a `CommandResult` envelope.

## Shape

```ts
interface CommandResult<T = void> {
    isSuccess: boolean;          // true when authorized + valid + no exceptions
    isAuthorized: boolean;       // false → user lacks permission
    isValid: boolean;            // false → one or more validators failed
    validationResults: ValidationResult[];
    hasExceptions: boolean;      // true → unhandled server exception
    exceptionMessages: string[];
    exceptionStackTrace: string;
    response: T | null;          // present when the backend returned a value
}

interface ValidationResult {
    propertyName: string;   // camelCase, matches the command property
    message: string;
    severity: string;       // 'Error' | 'Warning' | 'Info'
}
```

## Handling all cases

```tsx
const handleSubmit = async () => {
    const result = await command.execute();

    if (!result.isAuthorized) {
        navigate('/login');
        return;
    }

    if (!result.isValid) {
        // Map errors by property for inline display
        const fieldErrors = result.validationResults.reduce((acc, v) => {
            acc[v.propertyName] = v.message;
            return acc;
        }, {} as Record<string, string>);
        setErrors(fieldErrors);
        return;
    }

    if (result.hasExceptions) {
        toast.error(result.exceptionMessages.join('\n'));
        return;
    }

    onSuccess(result.response);  // result.response typed when backend returns a value
};
```

## Accessing the returned value

If the backend controller returns a value:

```csharp
[HttpPost]
public async Task<Guid> CreateOrder([FromBody] CreateOrder command)
{
    var id = Guid.NewGuid();
    await eventLog.Append(id, new OrderCreated(...));
    return id;
}
```

Then on the frontend:

```ts
const result = await createOrder.execute();
if (result.isSuccess) {
    const newId = result.response as string; // Guids come as strings
}
```

## Bypassing the CommandResult wrapper

If you need to return a raw HTTP response (e.g. for file downloads), use `[AspNetResult]` on the controller action. The frontend receives the raw response and the proxy does not include `execute()`.
