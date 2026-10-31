```csharp
BookStatus? status = await eventStore.ReadModels.GetInstanceById<BookStatus>(bookId);

if (status is not null)
{
    Console.WriteLine($"{status.Title}: {(status.IsBorrowed ? status.BorrowedBy : "on the shelf")}");
}
```
