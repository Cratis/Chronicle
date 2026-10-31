// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_redelivering_an_occurrence : given.an_occurrence_client
{
    readonly List<CorrelationId> _correlations = [];
    readonly List<IDictionary<string, string>> _causes = [];

    void Establish()
    {
        _handler.OnHandle = (_, _, _) =>
        {
            _correlations.Add(new CorrelationIdAccessor().Current);
            return Task.CompletedTask;
        };
        _causation.When(manager => manager.BeginScope(Arg.Any<CausationType>(), Arg.Any<IDictionary<string, string>>())).Do(call => _causes.Add(new Dictionary<string, string>(call.Arg<IDictionary<string, string>>())));
    }

    async Task Because()
    {
        var attempt = CreateAttempt(TimeProvider.System);
        await attempt.Run(_claim, CancellationToken.None);
        _claim.Epoch++;
        _claim.Attempts++;
        await attempt.Run(_claim, CancellationToken.None);
    }

    [Fact] void should_use_a_fresh_correlation_per_attempt() => _correlations.Distinct().Count().ShouldEqual(2);
    [Fact] void should_link_both_attempts_to_the_occurrence() => _causes.Select(cause => cause["occurrenceId"]).Distinct().Single().ShouldEqual("occurrence");
    [Fact] void should_record_the_fencing_epoch() => _causes[1]["epoch"].ShouldEqual("4");
    [Fact] void should_record_the_trigger() => _causes[1]["triggerId"].ShouldEqual("test-trigger");
}
