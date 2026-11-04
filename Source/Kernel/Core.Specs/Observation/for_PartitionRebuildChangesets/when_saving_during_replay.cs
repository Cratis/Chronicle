// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildChangesets;

public class when_saving_during_replay : Specification
{
    readonly ReadModelContainerName _container = "read-model";
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    PartitionRebuildChangesets _storage;

    void Establish()
    {
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _storage = new();
    }

    async Task Because()
    {
        await _storage.BeginReplay(_container);
        await _storage.Save(_container, new Key("partition", ArrayIndexers.NoIndexers), new EventType("Event", EventTypeGeneration.First), 1, Guid.NewGuid(), _changeset);
        await _storage.EndReplay(_container);
    }

    [Fact] void should_discard_the_changeset_without_reading_it() => _changeset.ReceivedCalls().ShouldBeEmpty();
}
