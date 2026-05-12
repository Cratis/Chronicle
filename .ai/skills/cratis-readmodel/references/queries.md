# Queries — Reference

## Query endpoint patterns

### Collection query

```csharp
[HttpGet]
public IEnumerable<AccountSummary> AllAccounts()
    => collection.Find(_ => true).ToList();
```

### Single item query

```csharp
[HttpGet("{id}")]
public AccountSummary? GetAccount(Guid id)
    => collection.Find(a => a.Id == id).FirstOrDefault();
```

### Filtered query (with proxy parameter)

```csharp
[HttpGet("search")]
public IEnumerable<AccountSummary> Search([FromQuery] string? filter)
    => collection.Find(a => a.Name.StartsWith(filter ?? string.Empty)).ToList();
```

The `[FromQuery]` parameter is included in the generated TypeScript proxy.

### Observable (real-time) query

Return `ISubject<T>` to push data to clients over WebSocket:

```csharp
[HttpGet("live")]
public ISubject<IEnumerable<AccountSummary>> AllAccountsLive()
{
    var observable = new ClientObservable<IEnumerable<AccountSummary>>();
    observable.OnNext(collection.Find(_ => true).ToList());

    var changeStream = collection.Watch();
    observable.ClientDisconnected += () => changeStream.Dispose();
    Task.Run(async () =>
    {
        await foreach (var _ in changeStream.ToAsyncEnumerable())
            observable.OnNext(collection.Find(_ => true).ToList());
    });

    return observable;
}
```

The proxy generator produces an `ObservableQuery` TypeScript class for `ISubject<T>` return types. The React hook `useObservableQuery()` is used automatically.

---

## QueryResult shape (frontend)

```ts
interface QueryResultWithState<T> {
    data: T;
    isSuccess: boolean;
    isAuthorized: boolean;
    isValid: boolean;
    validationResults: ValidationResult[];
    hasExceptions: boolean;
    exceptionMessages: string[];
    paging: { page: number; pageSize: number; totalItems: number; totalPages: number };

    // React-specific:
    hasData: boolean;       // non-null and non-empty
    isPerforming: boolean;  // request in flight
}
```

---

## React usage

```tsx
// Standard query — returns [result, requery]
const [accounts, refresh] = AllAccounts.use();

// With parameters
const [results] = Search.use({ filter: searchText });

// Observable query — returns [result] only (no manual refresh)
const [liveAccounts] = AllAccountsLive.use();
```

For full page layouts with tables and menu actions, see the `cratis-react-page` skill.

---

## Naming conventions

The **method name** on the controller becomes the TypeScript proxy class name. Make it descriptive.

| ✅ Good | ❌ Avoid |
| ------- | ------- |
| `AllAccounts` | `Get`, `GetAll`, `List` |
| `AccountsByOwner` | `Query`, `Fetch` |
| `AllAccountsLive` | `Observable`, `Live` |
