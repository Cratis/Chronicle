// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Specifications.Auditing;
using Aksio.Specifications;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents the test context for an <see cref="IImporter"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <typeparam name="TExternalModel">Type of external model.</typeparam>
public class AdapterSpecificationContext<TModel, TExternalModel> : IHaveEventLog, IHaveEventOutbox, IDisposable
{
    readonly IImportOperations<TModel, TExternalModel> _importOperations;
    readonly ProjectionSpecificationContext<TModel> _projectionSpecificationContext;
    readonly EventOutboxForSpecifications _outbox = new();
    int _eventCountBeforeImport;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterSpecificationContext{TModel, TExternalModel}"/> class.
    /// </summary>
    /// <param name="adapter"><see cref="IAdapterFor{TModel, TExternalModel}"/> instance.</param>
    public AdapterSpecificationContext(IAdapterFor<TModel, TExternalModel> adapter)
    {
        var objectComparer = new ObjectComparer();
        _projectionSpecificationContext = new ProjectionSpecificationContext<TModel>(adapter.Identifier.Value, adapter.DefineModel);
        Projection = new SpecificationAdapterProjectionFor<TModel>(_projectionSpecificationContext);
        var adapterMapperFactory = new AdapterMapperFactory();
        var mapper = adapterMapperFactory.CreateFor(adapter);

        _importOperations = new ImportOperations<TModel, TExternalModel>(
            adapter,
            Projection,
            mapper,
            objectComparer,
            EventLog,
            EventOutbox,
            new NullCausationManager());
    }

    /// <inheritdoc/>
    public IEventLog EventLog => _projectionSpecificationContext.EventLog;

    /// <inheritdoc/>
    public IEventOutbox EventOutbox => _outbox;

    /// <inheritdoc/>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _projectionSpecificationContext.AppendedEvents;

    /// <inheritdoc/>
    public IEnumerable<AppendedEventForSpecifications> AppendedEventsToOutbox => _outbox.AppendedEvents;

    /// <summary>
    /// Gets the <see cref="IAdapterProjectionFor{TModel}"/> used.
    /// </summary>
    public IAdapterProjectionFor<TModel> Projection { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        _importOperations.Dispose();
        _projectionSpecificationContext.Dispose();
    }

    /// <summary>
    /// Import the instance of the external model.
    /// </summary>
    /// <param name="externalModel">External model to import.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Import(TExternalModel externalModel)
    {
        _eventCountBeforeImport = _projectionSpecificationContext.AppendedEvents.Count();
        await _importOperations.Apply(externalModel);
    }

    /// <summary>
    /// Assert that there has not been added any events as the result of an import.
    /// </summary>
    public void ShouldNotAppendEventsDuringImport() => AppendedEvents.Count().ShouldEqual(_eventCountBeforeImport);
}
