// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.Copy.for_CopyEventSourceEventsStep;

public class when_performing_and_the_append_is_refused : given.the_job_step
{
    void Establish() =>
        _sequence.AppendManyWithIdempotencyKeys(
            Arg.Any<IEnumerable<EventToAppend>>(),
            Arg.Any<IReadOnlyList<IdempotencyKey?>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<ConcurrencyScopes>()).Returns(Task.FromResult(new IdempotentAppendManyResult(
                new AppendManyResult { Errors = [AppendError.ConstraintIndexUnavailable] })));

    async Task Because() => await Perform();

    [Fact] void should_fail_the_step() => _result.TryGetException(out _).ShouldBeTrue();
}
