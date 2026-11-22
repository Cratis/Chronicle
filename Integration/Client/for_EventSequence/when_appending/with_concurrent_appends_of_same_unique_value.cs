// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_concurrent_appends_of_same_unique_value.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_concurrent_appends_of_same_unique_value(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Event { get; private set; }

        public IReadOnlyList<IAppendResult> Results { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }

        public void Establish() => Event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        public async Task Because()
        {
            string[] eventSourceIds = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()];
            var results = await Task.WhenAll(eventSourceIds.Select(id => EventStore.EventLog.Append(id, Event)));
            Results = results;

            // Only select outcome metadata, never the event or violation messages/details (which can contain values).
            Diagnostics = results.Select((result, index) => Sanitize(JsonSerializer.Serialize(new
            {
                EventSourceId = eventSourceIds[index],
                CorrelationId = result.CorrelationId.Value,
                result.IsSuccess,
                Errors = result.Errors.Select(error => error.Value),
                ConstraintViolationCount = result.ConstraintViolations.Count(),
                ConstraintNames = result.ConstraintViolations.Select(violation => violation.ConstraintName.Value),
                result.ConcurrencyViolation
            }))).ToArray();
        }

        string Sanitize(string diagnostics) => diagnostics
            .Replace(Event.UserName, "[redacted]", StringComparison.Ordinal)
            .Replace(Event.Name, "[redacted]", StringComparison.Ordinal);
    }

    [Fact]
    void should_have_exactly_one_successful_append()
    {
        WriteDiagnosticsIfCountsAreWrong();
        Context.Results.Count(_ => _.IsSuccess).ShouldEqual(1);
    }

    [Fact]
    void should_have_exactly_one_append_with_a_constraint_violation()
    {
        WriteDiagnosticsIfCountsAreWrong();
        Context.Results.Count(_ => _.HasConstraintViolations).ShouldEqual(1);
    }

    void WriteDiagnosticsIfCountsAreWrong()
    {
        if (Context.Results.Count(_ => _.IsSuccess) == 1 && Context.Results.Count(_ => _.HasConstraintViolations) == 1) return;

        foreach (var diagnostics in Context.Diagnostics)
        {
            TestContext.Current.TestOutputHelper!.WriteLine(diagnostics);
        }
    }
}
