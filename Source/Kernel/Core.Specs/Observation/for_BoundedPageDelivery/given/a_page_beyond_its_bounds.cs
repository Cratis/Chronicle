// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery.given;

public class a_page_beyond_its_bounds : Specification
{
    protected static readonly Key _first = "first";
    protected static readonly Key _second = "second";
    protected readonly PageBounds _bounds = new(2, int.MaxValue);
    protected readonly List<IReadOnlyList<ObserverPartitionBatch>> _delivered = [];
    protected ObserverPartitionBatch[] _batches;

    void Establish() => _batches =
    [
        new ObserverPartitionBatch(_first, [AppendedEvent.EmptyWithEventSequenceNumber(1), AppendedEvent.EmptyWithEventSequenceNumber(2), AppendedEvent.EmptyWithEventSequenceNumber(3)]),
        new ObserverPartitionBatch(_second, [AppendedEvent.EmptyWithEventSequenceNumber(4)])
    ];

    protected Task<ObserverPageResult> Answer(IReadOnlyList<ObserverPartitionBatch> page, Func<ObserverPartitionBatch, ObserverSubscriberResult> resultFor)
    {
        _delivered.Add(page);
        return Task.FromResult(new ObserverPageResult(page.ToDictionary(_ => _.Partition, resultFor)));
    }

    protected static ObserverSubscriberResult HandledAll(ObserverPartitionBatch batch) => ObserverSubscriberResult.Ok(batch.Events[^1].Context.SequenceNumber);
}
