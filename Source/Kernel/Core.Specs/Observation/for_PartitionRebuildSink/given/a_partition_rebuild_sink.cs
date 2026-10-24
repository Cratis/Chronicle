// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink.given;

public class a_partition_rebuild_sink : Specification
{
    protected readonly Key _partition = new("partition", ArrayIndexers.NoIndexers);
    internal PartitionRebuildSink _sink;

    void Establish() => _sink = new(_partition);

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetSetting(string name, string value, ExpandoObject? initial = null)
    {
        initial ??= new ExpandoObject();
        var context = EventContext.From("store", "tenant", new("Source", EventTypeGeneration.First), EventSourceType.Default, "partition", EventStreamType.All, EventStreamId.Default, 0, Guid.NewGuid(), null, DateTimeOffset.UtcNow, "subject");
        var comparer = new ObjectComparer();
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, new AppendedEvent(context, new ExpandoObject()), initial);
        var next = new ExpandoObject();
        ((IDictionary<string, object?>)next)[name] = value;
        comparer.Compare(initial, next, out var differences);
        changeset.ReplaceState(next, differences);
        return changeset;
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetRemoving(ExpandoObject initial)
    {
        var context = EventContext.From("store", "tenant", new("Source", EventTypeGeneration.First), EventSourceType.Default, "partition", EventStreamType.All, EventStreamId.Default, 1, Guid.NewGuid(), null, DateTimeOffset.UtcNow, "subject");
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), new AppendedEvent(context, new ExpandoObject()), initial);
        changeset.Remove();
        return changeset;
    }
}
