// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImportOperations{TModel, TExternalModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model the operations are for.</typeparam>
/// <typeparam name="TExternalModel">Type of external model the operations are for.</typeparam>
public class ImportOperations<TModel, TExternalModel> : IImportOperations<TModel, TExternalModel>
{
    readonly Subject<ImportContext<TModel, TExternalModel>> _importContexts;
    readonly IObjectsComparer _objectsComparer;
    readonly IEventLog _eventLog;
    readonly IEventOutbox _eventOutbox;

    /// <inheritdoc/>
    public IAdapterFor<TModel, TExternalModel> Adapter { get; }

    /// <inheritdoc/>
    public IAdapterProjectionFor<TModel> Projection { get; }

    /// <inheritdoc/>
    public IMapper Mapper { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportOperations{TModel, TExternalModel}"/> class.
    /// </summary>
    /// <param name="adapter">The <see cref="IAdapterFor{TModel, TExternalModel}"/>.</param>
    /// <param name="adapterProjection">The <see cref="IAdapterProjectionFor{TModel}"/> for the model.</param>
    /// <param name="mapper"><see cref="IMapper"/> to use for mapping between external model and model.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
    /// <param name="eventLog">The <see cref="IEventLog"/> for appending private events.</param>
    /// <param name="eventOutbox">The <see cref="IEventOutbox"/> for appending public events.</param>
    public ImportOperations(
        IAdapterFor<TModel, TExternalModel> adapter,
        IAdapterProjectionFor<TModel> adapterProjection,
        IMapper mapper,
        IObjectsComparer objectsComparer,
        IEventLog eventLog,
        IEventOutbox eventOutbox)
    {
        Adapter = adapter;
        Projection = adapterProjection;
        Mapper = mapper;
        _objectsComparer = objectsComparer;
        _importContexts = new();
        Adapter.DefineImport(new ImportBuilderFor<TModel, TExternalModel>(_importContexts));
        _eventLog = eventLog;
        _eventOutbox = eventOutbox;
    }

    /// <inheritdoc/>
    public async Task Apply(TExternalModel instance)
    {
        var keyValue = Adapter.KeyResolver(instance)!;
        var eventSourceId = keyValue;
        eventSourceId ??= new(keyValue.ToString()!);
        var initial = await Projection.GetById(eventSourceId!);
        var mappedInstance = Mapper.Map<TModel>(instance)!;
        var changeset = new Changeset<TModel, TModel>(_objectsComparer, mappedInstance, initial);

        if (!_objectsComparer.Equals(initial, mappedInstance, out var differences))
        {
            changeset.Add(new PropertiesChanged<TModel>(mappedInstance, differences));
        }

        var context = new ImportContext<TModel, TExternalModel>(changeset, new EventsToAppend());
        _importContexts.OnNext(context);

        foreach (var @event in context.Events)
        {
            await _eventLog.Append(eventSourceId!, @event);

            if (@event.GetType().GetCustomAttribute<EventTypeAttribute>()?.IsPublic ?? false)
            {
                await _eventOutbox.Append(eventSourceId!, @event);
            }
        }
    }

    /// <inheritdoc/>
    public async Task Apply(IEnumerable<TExternalModel> instances)
    {
        foreach (var instance in instances)
        {
            await Apply(instance);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _importContexts.Dispose();
}
