// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_reopening_an_unreviewed_row(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    bool _reopened;
    ClosedStream? _sameSpelling;
    ClosedStream? _otherSpelling;
    bool _reopenedAfterReview;
    ClosedStream? _afterReview;
    ClosedStream? _reviewed;
    ClosedStream[] _covering;
    ConstraintValidationResult _pendingBatch;
    ConstraintValidationResult _reviewedBatch;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice"), "owner", 1, null));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 0");
        _pendingBatch = await ValidateBatchReopen();
        _reopened = await _harness.ClosedStreams.Reopen("owner", new(EventSourceId: "alice"));
        _sameSpelling = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _otherSpelling = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice"));
        _covering = [.. await _harness.ClosedStreams.GetCovering(new(EventSourceId: "alice"), [ClosedStreamDimensions.EventSourceId])];
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 1");
        _reviewed = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _reviewedBatch = await ValidateBatchReopen();
        _reopenedAfterReview = await _harness.ClosedStreams.Reopen("owner", new(EventSourceId: "alice"));
        _afterReview = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
    }

    [Fact] void should_keep_the_unreviewed_closure_in_batch_validation() => _pendingBatch.IsValid.ShouldBeFalse();
    [Fact] void should_allow_the_reviewed_closure_to_reopen_within_a_batch() => _reviewedBatch.IsValid.ShouldBeTrue();
    [Fact] void should_report_nothing_reopened() => _reopened.ShouldBeFalse();
    [Fact] void should_keep_the_stream_closed() => _sameSpelling.ShouldNotBeNull();
    [Fact] void should_keep_the_other_spelling_closed() => _otherSpelling.ShouldNotBeNull();
    [Fact] void should_mark_the_stored_closure_as_pending_review() => _sameSpelling!.PendingReview.ShouldBeTrue();
    [Fact] void should_mark_covering_closures_as_pending_review() => _covering.Single().PendingReview.ShouldBeTrue();
    [Fact] void should_clear_the_flag_after_review() => _reviewed!.PendingReview.ShouldBeFalse();
    [Fact] void should_reopen_after_review() => _reopenedAfterReview.ShouldBeTrue();
    [Fact] void should_leave_the_stream_open_after_review() => _afterReview.ShouldBeNull();

    async Task<ConstraintValidationResult> ValidateBatchReopen()
    {
        var storage = _harness.ClosedStreams;
        var definition = new ClosesStreamConstraintDefinition("owner", ["Closed"], ClosedStreamDimensions.EventSourceId, ["Reopened"]);
        var claims = new ConstraintBatchClaims();
        var reopening = new ConstraintValidationContext([], "alice", "Reopened", new ExpandoObject(), batchClaims: claims);
        ((IObserveConstraintBatchEvents)new ClosesStreamConstraintValidator(definition, storage, EventSequenceId.Log)).RecordBatchEvent(reopening);
        var appending = new ConstraintValidationContext([], "alice", "Appended", new ExpandoObject(), batchClaims: claims);
        return await new ClosedStreamConstraintValidator(storage, [ClosedStreamDimensions.EventSourceId], [definition]).Validate(appending);
    }
}
