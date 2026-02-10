# Counters

Counters track how many times events occur or increment/decrement numeric values. Chronicle provides three counter operations: `count`, `increment`, and `decrement`.

## Count

The `count` operation increments a property by 1 each time an event occurs:

```pdl
count {Property}
```

### Example

```pdl
from PageViewed
  count ViewCount
  LastViewedAt = $eventContext.occurred
```

Each `PageViewed` event increases `ViewCount` by 1.

## Increment

The `increment` operation increases a property by 1:

```pdl
increment {Property}
```

### Example

```pdl
from UserLoggedIn
  increment LoginCount
  LastLogin = $eventContext.occurred
```

## Decrement

The `decrement` operation decreases a property by 1:

```pdl
decrement {Property}
```

### Example

```pdl
from ItemConsumed
  decrement RemainingStock
  LastConsumed = $eventContext.occurred
```

## Difference Between Count and Increment

While `count` and `increment` both add 1, they have semantic differences:

- **`count`**: Counts occurrences of a specific event (conceptually counting events)
- **`increment`**: Increases a value (conceptually modifying state)

In practice, they behave identically but express different intent.

## Multiple Counters

You can use multiple counter operations in the same event:

```pdl
from OrderPlaced
  increment TotalOrders
  count OrderCount
  add TotalRevenue by total
```

## Examples

### User Activity Tracking

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email

  from UserLoggedIn
    count LoginCount
    LastLogin = $eventContext.occurred

  from UserProfileViewed
    increment ProfileViewCount
```

### Inventory Management

```pdl
projection Product => ProductReadModel
  from ProductCreated
    Name = name
    StockLevel = initialStock

  from ItemAdded
    increment StockLevel

  from ItemSold
    decrement StockLevel
    count SalesCount

  from ItemReturned
    increment StockLevel
    decrement SalesCount
```

### Blog Post Engagement

```pdl
projection BlogPost => BlogPostReadModel
  from PostPublished
    Title = title
    Content = content

  from PostViewed
    count ViewCount

  from PostLiked
    increment LikeCount

  from PostUnliked
    decrement LikeCount

  from CommentAdded
    count CommentCount
```

### Account Balance (Alternative to Arithmetic)

```pdl
projection Account => AccountReadModel
  from AccountOpened
    AccountNumber = accountNumber
    Balance = initialBalance

  from DepositMade
    increment TransactionCount
    add Balance by amount

  from WithdrawalMade
    increment TransactionCount
    subtract Balance by amount
```

### Forum User Reputation

```pdl
projection ForumUser => ForumUserReadModel
  from UserJoined
    Username = username

  from QuestionPosted
    count QuestionsPosted

  from AnswerPosted
    count AnswersPosted

  from UpvoteReceived
    increment Reputation

  from DownvoteReceived
    decrement Reputation
```

### Task Completion Tracking

```pdl
projection Project => ProjectReadModel
  from ProjectCreated
    Name = name

  from TaskAdded
    increment TotalTasks

  from TaskCompleted
    increment CompletedTasks
    decrement RemainingTasks

  from TaskAdded
    increment RemainingTasks
```

## Property Requirements

The target property must be a numeric type:
- `int`
- `long`
- `decimal`
- `double`
- `float`

Attempting to use counters on non-numeric properties will result in a validation error.

## Initial Values

Counter properties should have an initial value:

```csharp
public class UserReadModel
{
    public string Name { get; set; }
    public int LoginCount { get; set; } = 0;  // Initialize to 0
    public int ProfileViews { get; set; } = 0;
}
```

## Best Practices

1. **Initialize to Zero**: Ensure counter properties start at 0 in the read model
2. **Use Count for Events**: Use `count` when tracking event occurrences
3. **Use Increment/Decrement for State**: Use `increment`/`decrement` for state changes
4. **Combine with Timestamps**: Pair counters with timestamps for richer tracking
5. **Validate Decrements**: Ensure decrements won't result in negative values if that's not valid for your domain
6. **Consider Arithmetic**: For more complex numeric operations, use [Arithmetic](arithmetic.md) operations

## See Also

- [Arithmetic](arithmetic.md) - Add and subtract operations with variable amounts
- [Property Mapping](property-mapping.md) - Setting properties to specific values
