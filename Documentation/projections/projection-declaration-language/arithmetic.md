# Arithmetic Operations

Arithmetic operations allow you to add or subtract values from numeric properties using values from events. Unlike counters that always change by 1, arithmetic operations use variable amounts.

## Add

The `add` operation increases a property by a specified amount:

```pdl
add {Property} by {expression}
```pdl

### Example

```pdl
from PaymentReceived
  add Balance by amount
  LastPayment = $eventContext.occurred
```pdl

## Subtract

The `subtract` operation decreases a property by a specified amount:

```pdl
subtract {Property} by {expression}
```pdl

### Example

```pdl
from WithdrawalMade
  subtract Balance by amount
  LastWithdrawal = $eventContext.occurred
```pdl

## Expression Values

The expression after `by` can be:
- Event property: `amount`, `value`, `cost.total`
- Literal: `100`, `10.50`
- Template: `` `${baseAmount}` ``

## Multiple Operations

Combine multiple operations in one event:

```pdl
from OrderPlaced
  add TotalRevenue by total
  add OrderCount by 1
  increment TotalOrders
```pdl

## Examples

### Account Balance

```pdl
projection Account => AccountReadModel
  from AccountOpened
    AccountNumber = accountNumber
    Balance = initialBalance

  from DepositMade
    add Balance by amount
    LastDeposit = $eventContext.occurred

  from WithdrawalMade
    subtract Balance by amount
    LastWithdrawal = $eventContext.occurred

  from InterestApplied
    add Balance by interestAmount
```pdl

### Loyalty Points

```pdl
projection Customer => CustomerReadModel
  from CustomerRegistered
    Name = name
    Points = 0

  from PurchaseMade
    add Points by pointsEarned

  from PointsRedeemed
    subtract Points by pointsUsed
    LastRedemption = $eventContext.occurred
```pdl

### Inventory with Variable Quantities

```pdl
projection Product => ProductReadModel
  from ProductCreated
    Name = name
    StockLevel = initialStock

  from StockAdded
    add StockLevel by quantity
    LastRestocked = $eventContext.occurred

  from StockSold
    subtract StockLevel by quantity

  from StockAdjusted
    add StockLevel by adjustmentAmount
```pdl

### Budget Tracking

```pdl
projection Budget => BudgetReadModel
  from BudgetCreated
    Category = category
    AllocatedAmount = amount
    RemainingAmount = amount

  from ExpenseRecorded
    subtract RemainingAmount by cost
    add TotalSpent by cost

  from BudgetIncreased
    add AllocatedAmount by additionalAmount
    add RemainingAmount by additionalAmount
```pdl

### Order Totals

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    Subtotal = 0
    Tax = 0
    Total = 0

  from LineItemAdded
    add Subtotal by itemTotal
    add Total by itemTotal

  from TaxCalculated
    add Tax by taxAmount
    add Total by taxAmount

  from DiscountApplied
    subtract Subtotal by discountAmount
    subtract Total by discountAmount
```pdl

### Gaming Score

```pdl
projection PlayerScore => PlayerScoreReadModel
  from PlayerJoined
    PlayerName = name
    Score = 0

  from PointsEarned
    add Score by points
    LastScored = $eventContext.occurred

  from PointsLost
    subtract Score by points

  from BonusAwarded
    add Score by bonusPoints
    add BonusTotal by bonusPoints
```pdl

## Nested Properties

You can use nested properties from events:

```pdl
from TransactionProcessed
  add Balance by transaction.amount
  add TotalTransactions by 1
```pdl

## With Counters

Combine arithmetic with counters for comprehensive tracking:

```pdl
from SaleCompleted
  add TotalRevenue by saleAmount
  add TaxCollected by taxAmount
  increment SalesCount
  count CompletedTransactions
```pdl

## Property Requirements

The target property must be a numeric type:
- `int`
- `long`
- `decimal`
- `double`
- `float`

The expression value must be compatible with the target type.

## Error Handling

Chronicle handles arithmetic operations safely:
- Type mismatches result in compilation errors
- Ensure the event property used exists and is numeric
- Consider domain rules (e.g., balance can't go negative) in your application logic

## Best Practices

1. **Initialize Values**: Ensure numeric properties have initial values
2. **Use Appropriate Types**: Use `decimal` for money, `int` for counts
3. **Track Both Directions**: Pair `add` with corresponding `subtract` for bidirectional operations
4. **Audit Trail**: Combine with timestamps and counters for complete tracking
5. **Validation**: Consider adding validation in your domain to prevent invalid states
6. **Precision**: Be aware of floating-point precision issues with `double` and `float`

## Difference from Counters

| Operation | Amount | Use Case |
|-----------|--------|----------|
| `count` / `increment` / `decrement` | Always ±1 | Event counting, simple state changes |
| `add` / `subtract` | Variable from event | Balances, quantities, scores |

## See Also

- [Counters](counters.md) - Fixed increment/decrement operations
- [Property Mapping](property-mapping.md) - Setting properties to specific values
- [Expressions](expressions.md) - Understanding expression syntax
