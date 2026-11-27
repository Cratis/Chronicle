// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_an_identity_is_already_durable : given.a_fenced_append_storage
{
    Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure> _outcome;

    void Establish()
    {
        _writeFailure = Duplicate();
        _duringWrite = () => _foundEvents = _inserted.Select(item => item with { SequenceNumber = 99 }).ToArray();
    }

    async Task Because() => _outcome = await _storage.AppendManyWithIdentities(_context, [_event]);

    [Fact] void should_report_the_durable_identity() => _outcome.AsT1.AsT1.Receipts[_event.Identity!.Id].SequenceNumber.Value.ShouldEqual(99UL);
    [Fact] void should_reacquire_the_marker_before_receipt_lookup() => _steps.SequenceEqual(["fence", "insert", "abort", "fence", "commit"]).ShouldBeTrue();
    [Fact] void should_lookup_the_receipt_on_the_recovery_session() => _lookupSessions.Single().ShouldEqual(_sessions[1]);
    [Fact] void should_not_reuse_the_aborted_session() => _lookupSessions.Single().ShouldNotEqual(_writeSessions.Single());
}
