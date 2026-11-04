// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.given;

public class rebuild_sink
{
    public ISink Sink { get; } = Substitute.For<ISink>();
    public Dictionary<string, ExpandoObject> Documents { get; } = [];
    public List<Key> Finds { get; } = [];
    public int Writes { get; private set; }

    public rebuild_sink()
    {
        Sink.FindOrDefault(Arg.Any<Key>()).Returns(call =>
        {
            var key = call.Arg<Key>();
            Finds.Add(key);
            return Task.FromResult(Documents.GetValueOrDefault(key.Value.ToString()!)?.Clone());
        });
        Sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>()).Returns(call =>
        {
            var key = call.Arg<Key>();
            var id = key.Value.ToString()!;
            var changeset = call.Arg<IChangeset<AppendedEvent, ExpandoObject>>();
            Documents[id] = InMemoryChanges.Apply(key, changeset.Changes, Documents.GetValueOrDefault(id)?.Clone() ?? new ExpandoObject());
            Writes++;
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
    }
}
