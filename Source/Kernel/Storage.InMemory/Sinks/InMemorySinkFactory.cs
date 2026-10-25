// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.InMemory.Generations.Bindings;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for <see cref="InMemorySink"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySinkFactory"/> class.
/// </remarks>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
/// <param name="serviceProvider">Resolves generation storage lazily only for explicitly bound writers.</param>
public class InMemorySinkFactory(ITypeFormats typeFormats, IServiceProvider? serviceProvider) : ISinkFactory
{
    /// <summary>
    /// Initializes a factory for legacy sinks without resolving generation storage.
    /// </summary>
    /// <param name="typeFormats">The type formats.</param>
    public InMemorySinkFactory(ITypeFormats typeFormats) : this(typeFormats, null)
    {
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) => new InMemorySink(readModel, typeFormats);

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, GenerationSinkContext context)
    {
        var model = context.GetReadModel(eventStore, @namespace);
        if (model.Sink.Type != TypeId) throw new MaterializationBindingsNotSupported();
        var bindings = serviceProvider?.GetRequiredService<IMaterializationBindingStorage>() as InMemoryMaterializationBindingStorage
            ?? throw new MaterializationBindingsNotSupported();
        var destination = bindings.GetDestination(context.Grant, () => new InMemorySink(model, typeFormats));
        return new GenerationSink(bindings, context.Grant, destination);
    }

    sealed class GenerationSink(InMemoryMaterializationBindingStorage bindings, GenerationWriterGrant grant, InMemorySink destination) : ISink
    {
        public SinkTypeId TypeId => destination.TypeId;
        public bool SupportsIsolatedReplace => destination.SupportsIsolatedReplace;
        public async Task<ExpandoObject?> FindOrDefault(Key key) => (await destination.FindOrDefault(key))?.Clone();
        public async Task<ExpandoObject?> FindInPrimaryOrDefault(Key key) => (await destination.FindInPrimaryOrDefault(key))?.Clone();
        public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) => destination.TryFindRootKeyByChildValue(childPropertyPath, childValue);
        public async Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
        {
            var instances = await destination.GetInstances(occurrence, skip, take);
            return instances with { Instances = instances.Instances.Select(instance => instance.Clone()).ToArray() };
        }
        public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
            destination.ObserveInstances(occurrence, skip, take).Select(instances => instances.Select(instance => instance.Clone()).ToArray().AsEnumerable());
        public Task<IsolatedReplaceResult> ReplaceIsolated(Key key, ExpandoObject? state, EventSequenceNumber watermark) => bindings.WithGrant(grant, () => destination.ReplaceIsolated(key, state, watermark));
        public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) => bindings.WithGrant(grant, () => destination.ApplyChanges(key, changeset, eventSequenceNumber));
        public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode) => bindings.WithGrant(grant, () => destination.ApplyChanges(key, changeset, eventSequenceNumber, mode));
        public Task<IEnumerable<FailedPartition>> ApplyChangesImmediately(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode) => ApplyChanges(key, changeset, eventSequenceNumber, mode);
        public Task BeginBulk() => bindings.WithGrant(grant, destination.BeginBulk);
        public Task ResumeBulk() => BeginBulk();
        public Task<BulkFlushResult> FlushBulk() => bindings.WithGrant(grant, () => Task.FromResult(new BulkFlushResult(null, [])));
        public Task<IEnumerable<FailedPartition>> EndBulk() => bindings.WithGrant(grant, destination.EndBulk);
        public Task<IEnumerable<FailedPartition>> EndBulk(CancellationToken cancellationToken) => EndBulk();
        public Task EnsureIndexes() => bindings.WithGrant(grant, destination.EnsureIndexes);
        public Task BeginReplay(ReplayContext context) => throw new MaterializationBindingsNotSupported();
        public Task ResumeReplay(ReplayContext context) => throw new MaterializationBindingsNotSupported();
        public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => throw new MaterializationBindingsNotSupported();
        public Task LeaveReplay() => throw new MaterializationBindingsNotSupported();
        public Task Remove(ReadModelContainerName containerName) => throw new MaterializationBindingsNotSupported();
        public Task PrepareInitialRun() => throw new MaterializationBindingsNotSupported();
    }
}
