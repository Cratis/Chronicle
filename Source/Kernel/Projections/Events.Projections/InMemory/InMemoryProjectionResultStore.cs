// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Dynamic;

namespace Cratis.Events.Projections.InMemory
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStore"/> for working with projections in memory.
    /// </summary>
    public class InMemoryProjectionResultStore : IProjectionResultStore, IDisposable
    {
        /// <summary>
        /// Gets the identifier of the <see cref="InMemoryProjectionResultStore"/>.
        /// </summary>
        public static readonly ProjectionResultStoreTypeId ProjectionResultStoreTypeId = "8a23995d-da0b-4c4c-818b-f97992f26bbf";

        readonly Dictionary<object, ExpandoObject> _collection = new();
        readonly Dictionary<object, ExpandoObject> _rewindCollection = new();
        readonly Model _model;
        IProjectionResultStoreRewindScope? _rewindScope;

        /// <inheritdoc/>
        public ProjectionResultStoreTypeId TypeId => ProjectionResultStoreTypeId;

        /// <inheritdoc/>
        public ProjectionResultStoreTypeName Name => "InMemory";

        bool IsRewinding => _rewindScope != default;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryProjectionResultStore"/> class.
        /// </summary>
        /// <param name="model"><see cref="Model"/> the store is for.</param>
        public InMemoryProjectionResultStore(Model model)
        {
            _model = model;
        }

        /// <inheritdoc/>
        public Task<ExpandoObject> FindOrDefault(object key)
        {
            var collection = GetCollection();

            ExpandoObject modelInstance;
            if (collection.ContainsKey(key))
            {
                modelInstance = collection[key];
            }
            else
            {
                modelInstance = new ExpandoObject();
            }

            return Task.FromResult(modelInstance);
        }

        /// <inheritdoc/>
        public Task ApplyChanges(object key, IChangeset<Event, ExpandoObject> changeset)
        {
            var state = changeset.InitialState.Clone();

            foreach (var change in changeset.Changes)
            {
                state = state.OverwriteWith((change.State as ExpandoObject)!);
            }

            var collection = GetCollection();
            collection[key] = state;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public IProjectionResultStoreRewindScope BeginRewind() => _rewindScope = new InMemoryResultStoreRewindScope(_model);

        /// <inheritdoc/>
        public Task PrepareInitialRun()
        {
            GetCollection().Clear();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _rewindScope?.Dispose();
            GC.SuppressFinalize(this);
        }

        Dictionary<object, ExpandoObject> GetCollection() => IsRewinding ? _rewindCollection : _collection;
    }
}
