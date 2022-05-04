// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Specifications.Types;
using Aksio.Specifications;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents the test context for an <see cref="IImporter"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <typeparam name="TExternalModel">Type of external model.</typeparam>
public class AdapterSpecificationContext<TModel, TExternalModel> : IDisposable
{
    readonly IAdapterFor<TModel, TExternalModel> _adapter;
    readonly IImportOperations<TModel, TExternalModel> _importOperations;
    readonly EventLogForSpecifications _eventLog = new();
    int _eventCountBeforeImport;

    /// <summary>
    /// Gets the <see cref="IEventLog"/>.
    /// </summary>
    public IEventLog EventLog => _eventLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterSpecificationContext{TModel, TExternalModel}"/> class.
    /// </summary>
    /// <param name="adapter"><see cref="IAdapterFor{TModel, TExternalModel}"/> instance.</param>
    public AdapterSpecificationContext(IAdapterFor<TModel, TExternalModel> adapter)
    {
        _adapter = adapter;

        var schemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()));

        var builder = new ProjectionBuilderFor<TModel>(adapter.Identifier.Value, new EventTypesForSpecifications(), schemaGenerator);
        _adapter.DefineModel(builder);
        var projectionDefinition = builder.Build();
        var eventSequenceStorageProvider = new EventSequenceStorageProviderForSpecifications(_eventLog);
        var objectsComparer = new ObjectsComparer();
        var adapterProjectionFor = new SpecificationAdapterProjectionFor<TModel>(projectionDefinition, eventSequenceStorageProvider, objectsComparer);
        var adapterMapperFactory = new AdapterMapperFactory();
        var mapper = adapterMapperFactory.CreateFor(adapter);

        _importOperations = new ImportOperations<TModel, TExternalModel>(
            adapter,
            adapterProjectionFor,
            mapper,
            objectsComparer,
            _eventLog);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _importOperations.Dispose();
    }

    /// <summary>
    /// Import the instance of the external model.
    /// </summary>
    /// <param name="externalModel">External model to import.</param>
    public void Import(TExternalModel externalModel)
    {
        _eventCountBeforeImport = _eventLog.ActualEvents.Count();
        _importOperations.Apply(externalModel);
    }

    /// <summary>
    /// Assert that a set events are appended.
    /// </summary>
    /// <param name="events">Events to verify.</param>
    public void ShouldAppendEvents(params object[] events) => _eventLog.ActualEvents.ShouldContain(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="events">Events to verify.</param>
    public void ShouldOnlyAppendEvents(params object[] events) => _eventLog.ActualEvents.ShouldContainOnly(events);

    /// <summary>
    /// Assert that there has not been added any events as the result of an import.
    /// </summary>
    public void ShouldNotAppendEvents() => _eventLog.ActualEvents.Count().ShouldEqual(_eventCountBeforeImport);
}
