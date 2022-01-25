# Queries

Queries in the frontend is divided into the following:

- The underlying `IQueryFor<>`, `IObservableQueryFor<>` interfaces
- The React hooks; `useQuery()` and `useObservableQuery()``
- Proxy generator that generates TypeScript from the C# code to leverage the constructs.

## Proxy Generation

Starting with the latter; the [proxy generator](./proxy-generation.md) you'll get the queries generated directly to use
in the frontend. The proxies generated can be imported directly into your code.

## Usage

From a React component you can now use the static `use()` method:

```tsx
export const MyComponent = () => {
    const [accounts, queryAccounts] = AllAccounts.use();

    return (
        <>
        </>
    )
};
```

### Parameters

Queries can have parameters they can be used for instance for filtering.
Lets say you have a query called `StartingWith`:

```csharp
[HttpGet("starting-with")]
public IEnumerable<DebitAccount> StartingWith([FromQuery] string? filter)
{
    var filterDocument = Builders<DebitAccount>
        .Filter
        .Regex("name", $"^{filter ?? string.Empty}.*");

    return _collection.Find(filterDocument).ToList();
}
```

The `filter` parameter will be part of the generated proxy, since it has the `[FromQuery]`
attribute on it. Using the proxy requires you to now specify the parameter as well:

```tsx
export const MyComponent = () => {
    const [accounts, queryAccounts] = StartingWith.use({ filter: '' });

    return (
        <>
        </>
    )
};
```

> Note: Route values will also be considered parameters and generated when adorning
> a method parameter with `[HttpPost]`.
