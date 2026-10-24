// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.given;

public class a_legacy_sink : ISink
{
    public SinkTypeId TypeId => throw new NotSupportedException();
    public Task<ExpandoObject?> FindOrDefault(Key key) => throw new NotSupportedException();
    public Task<ExpandoObject?> FindInPrimaryOrDefault(Key key) => throw new NotSupportedException();
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) => throw new NotSupportedException();
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) => throw new NotSupportedException();
    public Task BeginBulk() => throw new NotSupportedException();
    public Task<IEnumerable<FailedPartition>> EndBulk() => throw new NotSupportedException();
    public Task BeginReplay(ReplayContext context) => throw new NotSupportedException();
    public Task ResumeReplay(ReplayContext context) => throw new NotSupportedException();
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => throw new NotSupportedException();
    public Task Remove(ReadModelContainerName containerName) => throw new NotSupportedException();
    public Task PrepareInitialRun() => throw new NotSupportedException();
    public Task EnsureIndexes() => throw new NotSupportedException();
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) => throw new NotSupportedException();
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) => throw new NotSupportedException();
}
