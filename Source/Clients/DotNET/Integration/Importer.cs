// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImporter"/>.
/// </summary>
public class Importer : IImporter
{
    readonly IObjectsComparer _objectsComparer;
    readonly IAdapters _adapters;
    readonly IEventLog _eventLog;
    readonly IEventOutbox _eventOutbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="Importer"/> class.
    /// </summary>
    /// <param name="adapters"><see cref="IAdapters"/> for getting <see cref="AdapterFor{TModel, TExternalModel}"/> instances.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
    /// <param name="eventLog"><see cref="IEventLog"/> for appending events.</param>
    /// <param name="eventOutbox"><see cref="IEventOutbox"/> for appending public events.</param>
    public Importer(
        IAdapters adapters,
        IObjectsComparer objectsComparer,
        IEventLog eventLog,
        IEventOutbox eventOutbox)
    {
        _objectsComparer = objectsComparer;
        _adapters = adapters;
        _eventLog = eventLog;
        _eventOutbox = eventOutbox;
    }

    /// <inheritdoc/>
    public IImportOperations<TModel, TExternalModel> For<TModel, TExternalModel>()
    {
        var adapter = _adapters.GetFor<TModel, TExternalModel>();
        var projection = _adapters.GetProjectionFor<TModel, TExternalModel>();
        var mapper = _adapters.GetMapperFor<TModel, TExternalModel>();
        return new ImportOperations<TModel, TExternalModel>(adapter, projection, mapper, _objectsComparer, _eventLog, _eventOutbox);
    }
}
