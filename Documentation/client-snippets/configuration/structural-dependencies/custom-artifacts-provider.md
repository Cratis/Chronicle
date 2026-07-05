```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record StructuralDepsBookBorrowed(string BookId);

[EventType]
public record StructuralDepsBookReturned(string BookId);

public record StructuralDepsBorrowedBook(string BookId);

public class StructuralDepsBorrowedBooksProjection : IProjectionFor<StructuralDepsBorrowedBook>
{
    public void Define(IProjectionBuilderFor<StructuralDepsBorrowedBook> builder) => builder
        .From<StructuralDepsBookBorrowed>(_ => _.Set(m => m.BookId).To(e => e.BookId));
}

public class StructuralDepsMyArtifactsProvider : IClientArtifactsProvider
{
    public IEnumerable<Type> EventTypes => [typeof(StructuralDepsBookBorrowed), typeof(StructuralDepsBookReturned)];
    public IEnumerable<Type> Projections => [typeof(StructuralDepsBorrowedBooksProjection)];
    public IEnumerable<Type> ModelBoundProjections => [];
    public IEnumerable<Type> Reactors => [];
    public IEnumerable<Type> Reducers => [];
    public IEnumerable<Type> ReactorMiddlewares => [];
    public IEnumerable<Type> ComplianceForTypesProviders => [];
    public IEnumerable<Type> ComplianceForPropertiesProviders => [];
    public IEnumerable<Type> AdditionalEventInformationProviders => [];
    public IEnumerable<Type> ConstraintTypes => [];
    public IEnumerable<Type> UniqueConstraints => [];
    public IEnumerable<Type> UniqueEventTypeConstraints => [];
    public IEnumerable<Type> RemoveConstraintEventTypes => [];
    public IEnumerable<Type> EventTypeMigrators => [];
    public IEnumerable<Type> EventSeeders => [];
}
```
