```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

// Uses the events and four implementations above. Call VerifyAll() from a test.
public static class ChoosingStyleVerification
{
    public static async Task VerifyAll()
    {
        await Verify<ChoosingStyleBookStatusModelBound>(m => new(m.Id, m.Title, m.Isbn, m.IsBorrowed, m.BorrowedBy));
        await Verify<ChoosingStyleBookStatusFluent>(m => new(m.Id, m.Title, m.Isbn, m.IsBorrowed, m.BorrowedBy));
        await Verify<ChoosingStyleBookStatusSelfReducing>(m => new(m.Id, m.Title, m.Isbn, m.IsBorrowed, m.BorrowedBy));
        await Verify<ChoosingStyleBookStatusReducerModel>(m => new(m.Id, m.Title, m.Isbn, m.IsBorrowed, m.BorrowedBy));
    }

    static async Task Verify<T>(Func<T, ChoosingStyleBookStatus> fields) where T : class
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(ChoosingStyleBookRegistered), typeof(ChoosingStyleBookBorrowed), typeof(ChoosingStyleBookReturned)],
            ModelBoundProjections = [typeof(ChoosingStyleBookStatusModelBound)],
            Projections = [typeof(ChoosingStyleBookStatusProjection)],
            Reducers = [typeof(ChoosingStyleBookStatusSelfReducing), typeof(ChoosingStyleBookStatusReducer)]
        };
        var defaults = new Defaults(artifacts);
        var registered = new ChoosingStyleBookRegistered("Dune", "9780441172719");
        var borrowed = new ChoosingStyleBookBorrowed("Avery");
        var returned = new ChoosingStyleBookReturned();
        var available = new ChoosingStyleBookStatus("book-42", "Dune", "9780441172719", false, null);

        var empty = new ReadModelScenario<T>(null, defaults);
        empty.Instance!.ShouldBeNull();

        await Check([registered], available);
        await Check([registered, borrowed], available with { IsBorrowed = true, BorrowedBy = "Avery" });
        await Check([registered, borrowed, returned], available);
        await Check([registered, borrowed, registered], available);
        await Check([borrowed], new("book-42", null, null, true, "Avery"));
        await Check([returned], new("book-42", null, null, false, null));

        async Task Check(object[] events, ChoosingStyleBookStatus expected)
        {
            var scenario = new ReadModelScenario<T>(null, defaults);
            await scenario.Given.ForEventSource("book-42").Events(events);
            fields(scenario.Instance!).ShouldEqual(expected);
        }
    }
}
```
