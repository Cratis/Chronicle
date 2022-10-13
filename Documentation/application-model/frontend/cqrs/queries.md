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

> Note: All data resulting from a query will be strongly typed based on the metadata provided by the proxy generator.
> You can read more about how serialization works [here](../../../fundamentals/serialization.md).

### Return tuple

If the query is a regular request / response type of query, the tuple returned contains two elements.
If it is an observable query, it only returns the first element of the tuple.

The return values are:

- The query result
- Delegate for issuing the query again

#### QueryResultWithState

The query result returned is a type called `QueryResultWithState` this is a sub type of `QueryResult`
adding properties that are relevant when working in React.

From the base `QueryResult` one gets the following properties:

| Property | Description |
| -------- | ----------- |
| data     | The actual data returned in the type expected. |
| isSuccess | Boolean telling whether or not the query was successful or not. |

On top of this `QueryResultWithState` adds the following properties:

| Property | Description |
| -------- | ----------- |
| hasData  | Boolean indicating whether or not there is data in the result. |
| isPerforming | Boolean that is true when an operation is working to get data from the server. |

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
