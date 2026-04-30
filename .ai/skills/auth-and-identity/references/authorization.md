# Authorization

This reference covers protecting commands and queries with authorization attributes.

## Attributes

Arc uses its own authorization attributes from `Cratis.Arc.Authorization` — these are **distinct from ASP.NET Core's** and are evaluated by `AuthorizationEvaluator` in the command/query pipeline.

| Attribute | Purpose |
|-----------|---------|
| `[Authorize]` | Require authentication. Optionally specify `Roles` or `Policy`. |
| `[Roles("Admin", "Manager")]` | Convenience wrapper — user needs at least **one** of the listed roles. |
| `[AllowAnonymous]` | Bypass authorization. Useful with fallback policies. |

## On Model-Bound Commands

```csharp
[Command]
[Roles("Admin", "Editor")]
public record DeleteArticle(ArticleId Id)
{
    public void Handle(IArticleService articles) => articles.Delete(Id);
}
```

When authorization fails, `Handle()` is **never called**. Check `CommandResult.IsAuthorized`:

```csharp
var result = await commandPipeline.Execute(new DeleteArticle(articleId));
if (!result.IsAuthorized)
{
    // User lacked required role — command was not executed
}
```

## On Model-Bound Queries

Authorization applies at both class and method level:

```csharp
[ReadModel]
[Authorize]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Roles("Admin")]
    public static IEnumerable<DebitAccount> GetAllAccounts(
        IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    [Roles("Manager")]
    public static IEnumerable<DebitAccount> GetHighValueAccounts(
        IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance > 50000).ToList();

    [AllowAnonymous]
    public static int GetTotalCount(IMongoCollection<DebitAccount> collection) =>
        (int)collection.CountDocuments(_ => true);
}
```

## Inheritance Rules

| Scenario | Result |
|----------|--------|
| `[Authorize]` on type | All methods require authentication |
| `[Roles]` on type | All methods require those roles |
| `[AllowAnonymous]` on type | All methods allow anonymous access |
| Method-level attribute present | **Overrides** type-level attribute |
| Both `[Authorize]` and `[AllowAnonymous]` on same target | **Error** — throws `AmbiguousAuthorizationLevel` |

Method-level always takes precedence:
- Methods **without** authorization attributes inherit the class-level attribute
- Methods **with** `[Roles(...)]` override the class-level attribute
- Methods **with** `[AllowAnonymous]` completely bypass authorization

## Fallback Policy (Secure by Default)

Make all endpoints require authentication unless explicitly opted out:

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

With this, use `[AllowAnonymous]` to make specific endpoints public:

```csharp
[Command]
[AllowAnonymous]
public record GetPublicCatalog()
{
    public Catalog Handle(ICatalogService catalog) => catalog.GetPublic();
}
```

### Default Policy vs Fallback Policy

| Policy | Applied When |
|--------|-------------|
| **Default Policy** | `[Authorize]` is used without parameters |
| **Fallback Policy** | No authorization attribute is present at all |

## Policy-Based Authorization

For complex scenarios:

```csharp
[Command]
[Authorize(Policy = "RequireElevatedAccess")]
public record PerformSensitiveOperation(string Data)
{
    public void Handle(ISensitiveService service) => service.Execute(Data);
}
```

Define the policy in startup:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireElevatedAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim("elevated", "true")));
});
```

## Custom Authorization Logic in Handlers

For domain-specific authorization beyond attributes:

```csharp
[Authorize]
public record UpdateOrder(OrderId Id, string Data)
{
    public async Task<CommandResult> Handle(
        IOrderRepository orders, CommandContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var order = await orders.GetById(Id);

        if (order.OwnerId != userId)
            return CommandResult.Forbidden(context.CorrelationId, "Can only update own orders");

        order.Update(Data);
        await orders.Save(order);
        return CommandResult.Success;
    }
}
```

## Authorization Results

| Scenario | HTTP Status |
|----------|-------------|
| Not authenticated | 401 Unauthorized |
| Authenticated but wrong role | 403 Forbidden |
