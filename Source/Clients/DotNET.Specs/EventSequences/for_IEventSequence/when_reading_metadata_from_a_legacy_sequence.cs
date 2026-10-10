// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_reading_metadata_from_a_legacy_sequence : given.a_legacy_event_sequence
{
    Exception _singleError;
    Exception _batchError;

    async Task Because()
    {
        _singleError = await Catch.Exception(() => _sequence.GetMetadataAt((EventSequenceNumber)42UL));
        _batchError = await Catch.Exception(() => _sequence.GetMetadataAt([42UL]));
    }

    [Fact] void should_refuse_the_single_read() => _singleError.ShouldBeOfExactType<EventMetadataReadsNotSupported>();
    [Fact] void should_refuse_the_batch_read() => _batchError.ShouldBeOfExactType<EventMetadataReadsNotSupported>();
}
