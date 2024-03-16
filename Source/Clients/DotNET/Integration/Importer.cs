// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Auditing;
using Cratis.Changes;
using Cratis.EventSequences;

namespace Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImporter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Importer"/> class.
/// </remarks>
/// <param name="adapters"><see cref="IAdapters"/> for getting <see cref="AdapterFor{TModel, TExternalModel}"/> instances.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> to compare objects with.</param>
/// <param name="eventLog"><see cref="IEventSequence"/> for appending events.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
public class Importer(
    IAdapters adapters,
    IObjectComparer objectComparer,
    IEventLog eventLog,
    ICausationManager causationManager) : IImporter
{
    /// <inheritdoc/>
    public IImportOperations<TModel, TExternalModel> For<TModel, TExternalModel>()
    {
        var adapter = adapters.GetFor<TModel, TExternalModel>();
        var projection = adapters.GetProjectionFor<TModel, TExternalModel>();
        var mapper = adapters.GetMapperFor<TModel, TExternalModel>();
        return new ImportOperations<TModel, TExternalModel>(adapter, projection, mapper, objectComparer, eventLog, causationManager);
    }
}
