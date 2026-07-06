```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Xunit;

[FromEvent<TestSliceBookAdded>]
public record TestSliceBook(
    [Key] Guid Id,
    string Title,
    [SetValue<TestSliceBookBorrowed>(true)] bool OnLoan,
    [SetFrom<TestSliceBookBorrowed>(nameof(TestSliceBookBorrowed.BorrowedBy))] string? BorrowedBy);

public class when_a_book_is_borrowed : Specification
{
    readonly TestSliceBookId _bookId = TestSliceBookId.New();
    readonly ReadModelScenario<TestSliceBook> _scenario = new();

    Task Because() =>
        _scenario.Given
            .ForEventSource(_bookId)
            .Events(
                new TestSliceBookAdded("The Pragmatic Programmer", "978-0135957059"),
                new TestSliceBookBorrowed("Ada Lovelace"));

    [Fact] void should_be_on_loan() => _scenario.Instance!.OnLoan.ShouldBeTrue();
    [Fact] void should_record_the_borrower() => _scenario.Instance!.BorrowedBy.ShouldEqual("Ada Lovelace");
}
```
