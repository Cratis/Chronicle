// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder.when_publishing;

public class and_the_empty_range_has_a_newer_live_write : given.a_rebuilder
{
    bool _result;
    bool _newerDocumentSurvives = true;

    async Task Establish()
    {
        // Model the storage watermark predicate: an unrestricted sentinel would incorrectly delete E+1.
        _liveSink.ReplaceIsolated(_partition, null, Arg.Any<EventSequenceNumber>()).Returns(call =>
        {
            var watermark = (EventSequenceNumber)call[2];
            if (watermark < (EventSequenceNumber)51UL) return IsolatedReplaceResult.Superseded;
            _newerDocumentSurvives = false;
            return IsolatedReplaceResult.Removed;
        });
        await _rebuilder.Begin(_observerKey, _partition);
    }

    async Task Because() => _result = await _rebuilder.Publish(50UL);

    [Fact] void should_be_superseded() => _result.ShouldBeFalse();
    [Fact] void should_leave_the_newer_document_intact() => _newerDocumentSurvives.ShouldBeTrue();
    [Fact] void should_guard_the_null_replace_at_the_finite_bound() => _liveSink.Received(1).ReplaceIsolated(_partition, null, (EventSequenceNumber)50UL);
}
