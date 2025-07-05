# Reducers

If you don't need the flexibility of a **Reactor** and are only interested in producing the correct current state, a **Reducer** is the ideal choice.
It functions similarly to a **Reactor** but abstracts away the database management, allowing Chronicle to handle it for you.

Let's begin by defining a read model that will be used in the reducer.

{{snippet:Quickstart-Book}}

For the read model we will need code that produces the correct state.
The following code reacts to `BookAddedToInventory` and produces the new state that should be persisted.

{{snippet:Quickstart-BooksReducer}}

The method `Added` is not defined by the `IReducerFor<>` interface. The `IReducerFor<>` interface serves as a marker interface for discovery purposes.
It requires a generic argument specifying the type of the read model. Chronicle uses this type to gather information about properties and types for the underlying database.

Methods in a reducer are convention-based, meaning they will be automatically detected and invoked as long as they adhere to the expected signature conventions.

Supported method signatures include:

```csharp
ReadModel <MethodName>(EventType @event, ReadModel? initialState);
ReadModel <MethodName>(EventType @event, , ReadModel? initialState, EventContext context);
Task<ReadModel> <MethodName>(EventType @event, ReadModel? initialState);
Task<ReadModel> <MethodName>(EventType @event, ReadModel? initialState, EventContext context);
```

> Note: Chronicle only supports MongoDB for reducers at the moment.

Opening your database client, you should be able to see the books:

![](./mongodb-books.png)
