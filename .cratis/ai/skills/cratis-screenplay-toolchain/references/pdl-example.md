<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/pdl-example.md -->
# Projection (PDL) example

Projection forms that compile warning-free, bind on both tools and run in the reference runner without Unsupported results (`PlacingAnOrder`, `CancellingAnOrder`, `Depositing` and `RejectingAnEmptyCode` pass). The specifications exercise every, from, `set`, increment, remove with, all + count, add/subtract by, `clear` and the concept rules; `PlacingAnOrder` also runs the `children lines` block (through `LineAdded`) without asserting it; join, nested and variant groups bind but no specification drives them. Forms in this example: every, join, children, nested, clear with, remove with, increment/decrement, all + count, add/subtract by, set to, clear, no automap, literal mapping, variant group (no `=> RM`), concept rules not empty/max/length ==. `lineCount` is initialized in `OrderPlaced` and `shipping` is optional, because the reference runner requires every non-optional read-model property to be set after each fact. Rejected by the binder: more than one every/all block on one level (PLAY0268); `$eventContext.occurred.Week` (PLAY0273, derived value). Not in this example because they block the whole execution plan in the reference runner (Screenplay v4.66.0): a `remove via join` on a projection's own (root) level (`UnsupportedProjectionBlock`; it binds, but on Chronicle v19.32.0 it creates no removal subscription, so `CustomerDeleted` would not remove an `OrderView`), and any event-context value other than the event source identity in a projection, such as `$eventContext.occurred` (`UnsupportedEventContext`). Quick reference: [cheat-sheet.md](cheat-sheet.md). The `every` block here maps a literal to show the form. Carry a timestamp on the event with `$context.occurred` in the producing command when a view must show it.

```screenplay
// PDL forms that bind executable on both tools.
concept OrderId : Uuid
concept CustomerId : Uuid
concept IssueId : Uuid
concept AccountId : Uuid
concept Code : String
  validate
    not empty  message "Required"
    max 10     message "Too long"
    length == 4
type OrderLine
  lineNumber Int
  sku        String
type Shipping
  carrier String optional
module Shop
  feature Orders
    slice StateChange PlaceOrder
      command PlaceOrder
        orderId    OrderId identifier
        customerId CustomerId
        total      Decimal
        produces event OrderPlaced
          customerId CustomerId = customerId
          total Decimal = total
        produces event LineAdded
          lineNumber Int = 1
          sku String = "A-1"
      specification PlacingAnOrder
        when PlaceOrder
          orderId    = "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
          customerId = "1b2c3d4e-5f60-4718-9a2b-3c4d5e6f7a8b"
          total      = 25
        then OrderPlaced
          for "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
          customerId = "1b2c3d4e-5f60-4718-9a2b-3c4d5e6f7a8b"
          total      = 25
        then LineAdded
          for "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
          lineNumber = 1
          sku        = "A-1"
        then query OrderViewById
          arguments
            orderId = "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
          result
            total     = 25
            status    = "placed"
            lastSeen  = "seen"
            lineCount = 1
    slice StateChange RemoveLine
      command RemoveLine
        orderId    OrderId identifier
        lineNumber Int
        produces event LineRemoved
          lineNumber Int = lineNumber
    slice StateChange ShipOrder
      command ShipOrder
        orderId OrderId identifier
        carrier String
        produces event Shipped
          carrier String = carrier
        produces event ShippingCleared
    slice StateChange CancelOrder
      command CancelOrder
        orderId OrderId identifier
        produces event OrderCancelled
      specification CancellingAnOrder
        given OrderPlaced
          for "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
          customerId = "1b2c3d4e-5f60-4718-9a2b-3c4d5e6f7a8b"
          total      = 25
        when CancelOrder
          orderId = "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
        then OrderCancelled
          for "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
        then no readmodel OrderView for "7a1c2e30-4b5d-4e6f-8a9b-0c1d2e3f4a5b"
    slice StateChange RenameCustomer
      command RenameCustomer
        customerId CustomerId identifier
        name       String
        produces event CustomerRenamed
          name String = name
        produces event CustomerDeleted
    slice StateView OrderOverview
      readmodel OrderView
        orderId      OrderId
        customerId   CustomerId
        customerName String optional
        total        Decimal
        lastSeen     String
        lineCount    Int
        status       String
        lines        OrderLine[]
        shipping     Shipping optional
      query OrderViewById => OrderView optional
        by orderId OrderId
      projection OrderOverview => OrderView
        no automap
        every
          lastSeen = "seen"
        from OrderPlaced
          orderId    = $eventSourceId
          customerId = customerId
          total      = total
          status     = "placed"
          set lineCount to 0
        from LineAdded
          increment lineCount
        from LineRemoved
          decrement lineCount
        join Customer on customerId
          with CustomerRenamed
            customerName = name
        children lines identified by lineNumber
          from LineAdded key lineNumber
            sku = sku
          remove with LineRemoved key lineNumber
        nested shipping
          from Shipped
            carrier = carrier
          clear with ShippingCleared
        remove with OrderCancelled
  feature Items

module Work
  feature Items
    slice StateChange OpenIssue
      command OpenIssue
        issueId IssueId identifier
        title   String
        produces event IssueCreated
          title String = title
        produces event IssueTitleCorrected
          title String = title
    slice StateChange OpenPullRequest
      command OpenPullRequest
        issueId IssueId identifier
        status  String
        produces event PullRequestCreated
        produces event BuildCompleted
          status String = status
    slice StateView WorkItems
      readmodel BacklogItem
        issueId IssueId
        title   String
      readmodel PullRequestItem
        issueId     IssueId
        title       String
        buildStatus String
      query BacklogItemById => BacklogItem optional
        by issueId IssueId
      query PullRequestItemById => PullRequestItem optional
        by issueId IssueId
      projection WorkItem
        from IssueTitleCorrected
          title = title
        variant BacklogItem
          enters on IssueCreated
        variant PullRequestItem
          enters on PullRequestCreated
          from BuildCompleted
            buildStatus = status

module Bank
  feature Accounts
    slice StateChange Deposit
      command Deposit
        accountId AccountId identifier
        amount    Decimal
        code      Code
        produces event Deposited
          amount Decimal = amount
        produces event Withdrawn
          amount Decimal = amount
        produces event Frozen
      specification Depositing
        when Deposit
          accountId = "5d1f0c2e-8a47-4c1b-9b3e-6f2a7c9d1e10"
          amount    = 10
          code      = "ABCD"
        then Deposited
          for "5d1f0c2e-8a47-4c1b-9b3e-6f2a7c9d1e10"
          amount = 10
        then Withdrawn
          for "5d1f0c2e-8a47-4c1b-9b3e-6f2a7c9d1e10"
          amount = 10
        then Frozen
          for "5d1f0c2e-8a47-4c1b-9b3e-6f2a7c9d1e10"
        then query AccountById
          arguments
            accountId = "5d1f0c2e-8a47-4c1b-9b3e-6f2a7c9d1e10"
          result
            balance  = 0
            deposits = 3
      specification RejectingAnEmptyCode           // one concept rejection spec, through this command
        when Deposit
          accountId = "5d1f0c2e-8a47-4c1b-9b3e-6f2a7c9d1e10"
          amount    = 10
          code      = ""
        then error "Required"
    slice StateView Balance
      readmodel Account
        accountId AccountId
        balance   Decimal
        deposits  Int
        note      String optional
      query AccountById => Account optional
        by accountId AccountId
      projection Balances => Account
        all
          count deposits
        from Deposited
          accountId = $eventSourceId
          add balance by amount
          set note to "active"
        from Withdrawn
          subtract balance by amount
        from Frozen
          clear note
```
