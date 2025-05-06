# Projections

Projections is a fully managed and declarative approach for describing how
to map from events and reduce it into the read state you want to represent.
It has capabilities that often are hard to do with imperative programming.
You can in many ways see it as an analogue to the concept of a materialized
view found in relational database.

The projection runs inside the Chronicle Kernel, which means that if your
client does not need to be running for it be active and handle events.

Things like one-to-one and one-to-many relationships of different event streams
are very easy to express with projections.

Given a read model like the following:

```csharp
public record MyReadModel(Guid Id, string Something);
```

And an event:

```csharp
public record MyEvent(string Something);
```

You can have the following simple projection:

```csharp
public class MyProjection : IProjectionFor<MyReadModel>
{
    public void Define(IProjectionBuilderFor<MyReadModel> builder) => builder
        .AutoMap()
        .From<MyEvent>();
}
```

By the virtue of implementing the `IProjectionFor<>`, the projection is
automatically discovered and registered with Chronicle.

A more concrete example of a reducer with one-to-one relationship:

{{snippet:Quickstart-BorrowedBooksProjection}}
