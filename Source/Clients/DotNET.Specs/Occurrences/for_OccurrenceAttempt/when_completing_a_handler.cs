// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_completing_a_handler : given.an_occurrence_client
{
    given.TestTrigger _trigger;
    OccurrenceContext _context;

    void Establish() => _handler.OnHandle = (trigger, context, _) =>
    {
        _trigger = trigger;
        _context = context;
        return Task.CompletedTask;
    };

    Task Because() => CreateAttempt(TimeProvider.System).Run(_claim, CancellationToken.None);

    [Fact] void should_deserialize_the_payload() => _trigger.Value.ShouldEqual("hello");
    [Fact] void should_supply_the_attempt_number() => _context.Attempt.ShouldEqual(2);
    [Fact] void should_supply_the_fenced_lease() => _context.Lease.ShouldEqual(new LeaseToken("occurrence", 3));
    [Fact] void should_complete_the_claim() => _service.Received(1).CompleteOccurrence(Arg.Is<CompleteOccurrenceRequest>(request => request.OccurrenceId == "occurrence" && request.Epoch == 3 && request.Namespace == "tenant"), Arg.Any<CallContext>());
    [Fact] void should_dispose_the_handler() => _handler.Disposed.ShouldBeTrue();
    [Fact] void should_not_fail_the_claim() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_clear_the_identity() => _identity.Received().ClearCurrentIdentity();
}
