// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_DecimalBulkIdentity;

public class when_redelivering_to_an_equivalent_key : given.a_decimal_bulk_sink
{
    async Task Establish() => await _sink.PrefetchPage([_original]);

    async Task Because() => await _sink.ApplyChanges(_equivalent, Changes(), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);

    [Fact] void should_skip_the_update_using_the_prefetched_watermark() => _changesetConverter.DidNotReceive().ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>());
}
