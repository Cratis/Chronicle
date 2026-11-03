// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising.given;

public class an_event_log_with_revised_unique_claims(ChronicleFixture fixture) : a_constrained_event_log(fixture)
{
    public override IEnumerable<Type> EventTypes => [typeof(NameReserved), typeof(CodeAssigned)];
    public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueReservedName), typeof(UniqueAssignedCode)];

    public EventSourceId Owner { get; } = "A";
    public EventSourceId OldValueClaimant { get; } = "B";
    public EventSourceId NewValueClaimant { get; } = "C";
    public EventSequenceNumber SequenceNumber { get; private set; } = EventSequenceNumber.Unavailable;
    public IAppendResult OriginalAppend { get; private set; } = null!;
    public IAppendResult OldValueAppend { get; private set; } = null!;
    public IAppendResult NewValueAppend { get; private set; } = null!;
    public IReadOnlyList<string> ClaimsBeforeRevision { get; private set; } = [];
    public IReadOnlyList<string> ClaimsAfterRevision { get; private set; } = [];
    public IReadOnlyList<string> ClaimsAfterReindex { get; private set; } = [];

    public async Task ReviseAndProbe<TEvent>(string constraintName, Func<string, TEvent> createEvent)
        where TEvent : notnull
    {
        var original = await EventStore.EventLog.Append(Owner, createEvent("alpha"));
        OriginalAppend = original;
        original.IsSuccess.ShouldBeTrue();
        SequenceNumber = original.SequenceNumber;
        ClaimsBeforeRevision = await Claims(constraintName, Owner, OldValueClaimant, NewValueClaimant);

        await this.ReviseEvent(SequenceNumber, createEvent("beta"));
        OldValueAppend = await AppendWhenIndexIsCurrent(() => EventStore.EventLog.Append(OldValueClaimant, createEvent("alpha")));
        NewValueAppend = await AppendWhenIndexIsCurrent(() => EventStore.EventLog.Append(NewValueClaimant, createEvent("beta")));
        ClaimsAfterRevision = await Claims(constraintName, Owner, OldValueClaimant, NewValueClaimant);

        await Reindex(constraintName);
        ClaimsAfterReindex = await Claims(constraintName, Owner, OldValueClaimant, NewValueClaimant);
    }
}
