// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImporter"/>.
/// </summary>
public class Importer : IImporter
{
    readonly IObjectComparer _objectComparer;
    readonly IAdapters _adapters;
    readonly IEventLog _eventLog;
    readonly IEventOutbox _eventOutbox;
    readonly ICausationManager _causationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Importer"/> class.
    /// </summary>
    /// <param name="adapters"><see cref="IAdapters"/> for getting <see cref="AdapterFor{TModel, TExternalModel}"/> instances.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> to compare objects with.</param>
    /// <param name="eventLog"><see cref="IEventSequence"/> for appending events.</param>
    /// <param name="eventOutbox"><see cref="IEventSequence"/> for appending public events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    public Importer(
        IAdapters adapters,
        IObjectComparer objectComparer,
        IEventLog eventLog,
        IEventOutbox eventOutbox,
        ICausationManager causationManager)
    {
        _objectComparer = objectComparer;
        _adapters = adapters;
        _eventLog = eventLog;
        _eventOutbox = eventOutbox;
        _causationManager = causationManager;
    }

    /// <inheritdoc/>
    public IImportOperations<TModel, TExternalModel> For<TModel, TExternalModel>()
    {
        var adapter = _adapters.GetFor<TModel, TExternalModel>();
        var projection = _adapters.GetProjectionFor<TModel, TExternalModel>();
        var mapper = _adapters.GetMapperFor<TModel, TExternalModel>();
        return new ImportOperations<TModel, TExternalModel>(adapter, projection, mapper, _objectComparer, _eventLog, _eventOutbox, _causationManager);
    }
}
