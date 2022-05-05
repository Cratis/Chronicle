// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Integration;
using Aksio.Specifications;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents the test context for an <see cref="IImporter"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <typeparam name="TExternalModel">Type of external model.</typeparam>
public class AdapterSpecificationContext<TModel, TExternalModel> : IDisposable
{
    readonly IImportOperations<TModel, TExternalModel> _importOperations;
    readonly ProjectionSpecificationContext<TModel> _projectionSpecificationContext;
    int _eventCountBeforeImport;

    /// <summary>
    /// Gets the <see cref="IEventLog"/>.
    /// </summary>
    public IEventLog EventLog => _projectionSpecificationContext.EventLog;

    /// <summary>
    /// Gets the <see cref="IAdapterProjectionFor{TModel}"/> used.
    /// </summary>
    public IAdapterProjectionFor<TModel> Projection { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterSpecificationContext{TModel, TExternalModel}"/> class.
    /// </summary>
    /// <param name="adapter"><see cref="IAdapterFor{TModel, TExternalModel}"/> instance.</param>
    public AdapterSpecificationContext(IAdapterFor<TModel, TExternalModel> adapter)
    {
        var objectsComparer = new ObjectsComparer();
        _projectionSpecificationContext = new ProjectionSpecificationContext<TModel>(adapter.Identifier.Value, adapter.DefineModel);
        Projection = new SpecificationAdapterProjectionFor<TModel>(_projectionSpecificationContext);
        var adapterMapperFactory = new AdapterMapperFactory();
        var mapper = adapterMapperFactory.CreateFor(adapter);

        _importOperations = new ImportOperations<TModel, TExternalModel>(
            adapter,
            Projection,
            mapper,
            objectsComparer,
            EventLog);
    }

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
        _eventCountBeforeImport = _projectionSpecificationContext._eventLog.ActualEvents.Count();
        await _importOperations.Apply(externalModel);
    }

    /// <summary>
    /// Assert that a set events are appended.
    /// </summary>
    /// <param name="events">Events to verify.</param>
    public void ShouldAppendEvents(params object[] events) => _projectionSpecificationContext._eventLog.ActualEvents.ShouldContain(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="events">Events to verify.</param>
    public void ShouldOnlyAppendEvents(params object[] events) => _projectionSpecificationContext._eventLog.ActualEvents.ShouldContainOnly(events);

    /// <summary>
    /// Assert that there has not been added any events as the result of an import.
    /// </summary>
    public void ShouldNotAppendEvents() => _projectionSpecificationContext._eventLog.ActualEvents.Count().ShouldEqual(_eventCountBeforeImport);
}
