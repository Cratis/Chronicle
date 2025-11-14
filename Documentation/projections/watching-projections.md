# Watching Projected Read Models

Chronicle provides real-time change notifications for projected read models through the `.Watch<TReadModel>()` API. This allows applications to respond immediately when projections are updated, enabling reactive UI updates and real-time integrations.

## Overview

The watch functionality allows you to subscribe to changes in projected read models using reactive streams (IObservable). When events are processed and projections are updated, subscribers receive notifications containing the updated read model and metadata about the change.

## Accessing the Watch API

The watch functionality is available through the `IEventStore.Projections` API:

```csharp
public class BookService
{
    private readonly IEventStore _eventStore;

    public BookService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public IObservable<ProjectionChangeset<BookInventory>> WatchBooks()
    {
        return _eventStore.Projections.Watch<BookInventory>();
    }
}
```

## ProjectionChangeset

When a projection is updated, the watch observable emits a `ProjectionChangeset<TReadModel>` containing:

- **ReadModelKey**: The unique identifier of the updated read model
- **ReadModel**: The complete updated read model instance
- **Namespace**: The event store namespace where the change occurred
- **Metadata**: Additional information about the change

```csharp
public class BookProjectionWatcher
{
    private readonly IEventStore _eventStore;
    private IDisposable? _subscription;

    public BookProjectionWatcher(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public void StartWatching()
    {
        _subscription = _eventStore.Projections.Watch<BookInventory>()
            .Subscribe(changeset =>
            {
                Console.WriteLine($"Book updated: {changeset.ReadModelKey}");
                Console.WriteLine($"Title: {changeset.ReadModel?.Title}");
                Console.WriteLine($"Quantity: {changeset.ReadModel?.Quantity}");
            });
    }

    public void StopWatching()
    {
        _subscription?.Dispose();
    }
}
```

## Filtering Changes

You can filter the change stream to only receive notifications for specific read models:

### Watch Specific Read Model by Key

```csharp
public IObservable<ProjectionChangeset<BookInventory>> WatchSpecificBook(Guid bookId)
{
    return _eventStore.Projections.Watch<BookInventory>()
        .Where(changeset => changeset.ReadModelKey == bookId.ToString());
}
```

### Watch by Criteria

```csharp
public IObservable<ProjectionChangeset<BookInventory>> WatchLowStockBooks()
{
    return _eventStore.Projections.Watch<BookInventory>()
        .Where(changeset => changeset.ReadModel?.Quantity < 10);
}
```

### Watch Multiple Read Model Types

```csharp
public class InventoryWatcher
{
    public void WatchInventoryChanges(IEventStore eventStore)
    {
        // Watch books
        eventStore.Projections.Watch<BookInventory>()
            .Subscribe(changeset => HandleBookUpdate(changeset.ReadModel));

        // Watch authors
        eventStore.Projections.Watch<AuthorProfile>()
            .Subscribe(changeset => HandleAuthorUpdate(changeset.ReadModel));
    }

    private void HandleBookUpdate(BookInventory? book) { /* ... */ }
    private void HandleAuthorUpdate(AuthorProfile? author) { /* ... */ }
}
```

## Real-Time UI Updates

The watch API is particularly useful for building reactive user interfaces:

### SignalR Integration

```csharp
[Hub]
public class BookHub : Hub
{
    private readonly IEventStore _eventStore;

    public BookHub(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task JoinBookUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "BookUpdates");

        // Start watching and broadcast to connected clients
        _eventStore.Projections.Watch<BookInventory>()
            .Subscribe(async changeset =>
            {
                await Clients.Group("BookUpdates").SendAsync("BookUpdated", new
                {
                    Id = changeset.ReadModelKey,
                    Book = changeset.ReadModel
                });
            });
    }
}
```

### Blazor Server Integration

```csharp
@page "/books"
@implements IDisposable
@inject IEventStore EventStore

<h3>Book Inventory (Real-time)</h3>

@foreach (var book in _books.Values)
{
    <div class="book-item">
        <h4>@book.Title</h4>
        <p>Quantity: @book.Quantity</p>
        <p>Last Updated: @book.LastUpdated</p>
    </div>
}

@code {
    private readonly Dictionary<string, BookInventory> _books = new();
    private IDisposable? _subscription;

    protected override async Task OnInitializedAsync()
    {
        // Load initial data
        // ... load existing books ...

        // Start watching for changes
        _subscription = EventStore.Projections.Watch<BookInventory>()
            .Subscribe(async changeset =>
            {
                if (changeset.ReadModel != null)
                {
                    _books[changeset.ReadModelKey] = changeset.ReadModel;
                    await InvokeAsync(StateHasChanged);
                }
            });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

## Background Services

Use the watch API in background services for automated reactions to projection changes:

```csharp
public class InventoryAlertService : BackgroundService
{
    private readonly IEventStore _eventStore;
    private readonly IEmailService _emailService;
    private readonly ILogger<InventoryAlertService> _logger;

    public InventoryAlertService(
        IEventStore eventStore,
        IEmailService emailService,
        ILogger<InventoryAlertService> logger)
    {
        _eventStore = eventStore;
        _emailService = emailService;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventStore.Projections.Watch<BookInventory>()
            .Where(changeset => changeset.ReadModel?.Quantity <= 5)
            .Subscribe(async changeset =>
            {
                _logger.LogInformation("Low stock detected for book: {BookId}", changeset.ReadModelKey);

                await _emailService.SendLowStockAlert(
                    changeset.ReadModel!.Title,
                    changeset.ReadModel.Quantity);
            }, stoppingToken);

        return Task.CompletedTask;
    }
}
```

## Error Handling

Implement proper error handling for watch subscriptions:

```csharp
public class RobustBookWatcher
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<RobustBookWatcher> _logger;
    private IDisposable? _subscription;

    public void StartWatching()
    {
        _subscription = _eventStore.Projections.Watch<BookInventory>()
            .Subscribe(
                onNext: changeset =>
                {
                    try
                    {
                        ProcessBookUpdate(changeset);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing book update: {BookId}", changeset.ReadModelKey);
                    }
                },
                onError: error =>
                {
                    _logger.LogError(error, "Watch stream error occurred");
                    // Consider implementing retry logic
                    RestartWatching();
                },
                onCompleted: () =>
                {
                    _logger.LogInformation("Watch stream completed");
                });
    }

    private void RestartWatching()
    {
        // Implement exponential backoff retry logic
        Task.Delay(TimeSpan.FromSeconds(5))
            .ContinueWith(_ => StartWatching());
    }
}
```

## Performance Considerations

### Subscription Management

```csharp
public class BookService : IDisposable
{
    private readonly CompositeDisposable _subscriptions = new();

    public void StartWatching(IEventStore eventStore)
    {
        // Add all subscriptions to composite for easy cleanup
        var bookSubscription = eventStore.Projections.Watch<BookInventory>()
            .Subscribe(HandleBookChange);

        _subscriptions.Add(bookSubscription);
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
```

### Batching Updates

```csharp
public class BatchedBookWatcher
{
    public void StartWatching(IEventStore eventStore)
    {
        eventStore.Projections.Watch<BookInventory>()
            .Buffer(TimeSpan.FromSeconds(1)) // Batch updates over 1 second
            .Where(batch => batch.Count > 0)
            .Subscribe(batch =>
            {
                ProcessBookBatch(batch);
            });
    }

    private void ProcessBookBatch(IList<ProjectionChangeset<BookInventory>> changes)
    {
        // Process multiple changes at once for efficiency
        foreach (var changeset in changes)
        {
            // Update UI, send notifications, etc.
        }
    }
}
```

## Best Practices

### 1. Manage Subscriptions Properly

- Always dispose of subscriptions to prevent memory leaks
- Use `CompositeDisposable` for managing multiple subscriptions
- Implement proper cleanup in service disposal

### 2. Handle Errors Gracefully

- Wrap update processing in try-catch blocks
- Log errors appropriately
- Implement retry mechanisms for transient failures

### 3. Filter Efficiently

- Filter at the observable level rather than in subscription handlers
- Use specific criteria to reduce unnecessary processing
- Consider performance impact of complex filters

### 4. Avoid Blocking Operations

- Use async/await for I/O operations in subscription handlers
- Don't perform heavy computations in subscription callbacks
- Consider using Task.Run for CPU-intensive work

### 5. Test Watch Functionality

```csharp
[Test]
public async Task Should_receive_book_updates()
{
    // Arrange
    var received = new List<ProjectionChangeset<BookInventory>>();
    var subscription = eventStore.Projections.Watch<BookInventory>()
        .Subscribe(changeset => received.Add(changeset));

    // Act
    await eventStore.EventLog.Append(bookId, new BookCreated("Test Book", "Test Author"));

    // Wait for projection to be processed
    await Task.Delay(100);

    // Assert
    received.Should().HaveCount(1);
    received[0].ReadModel?.Title.Should().Be("Test Book");

    subscription.Dispose();
}
```

## Summary

The `.Watch<TReadModel>()` API provides a powerful way to build reactive applications that respond immediately to projection changes. By leveraging this functionality, you can create responsive user interfaces, implement real-time notifications, and build automated systems that react to data changes as they happen.

Remember to manage subscriptions properly, handle errors gracefully, and test your watch functionality thoroughly to ensure robust real-time behavior in your applications.
