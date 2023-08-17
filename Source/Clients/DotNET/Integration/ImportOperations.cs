// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Reflection;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImportOperations{TModel, TExternalModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model the operations are for.</typeparam>
/// <typeparam name="TExternalModel">Type of external model the operations are for.</typeparam>
public class ImportOperations<TModel, TExternalModel> : IImportOperations<TModel, TExternalModel>
{
    /// <summary>
    /// The causation type for client observer.
    /// </summary>
    public static readonly CausationType CausationType = new("Import Operations");

    readonly Subject<ImportContext<TModel, TExternalModel>> _importContexts;
    readonly IObjectsComparer _objectsComparer;
    readonly IEventSequence _eventLog;
    readonly IEventSequence _eventOutbox;
    readonly ICausationManager _causationManager;

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
    /// <param name="eventLog">The <see cref="IEventSequence"/> for appending private events.</param>
    /// <param name="eventOutbox">The <see cref="IEventSequence"/> for appending public events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    public ImportOperations(
        IAdapterFor<TModel, TExternalModel> adapter,
        IAdapterProjectionFor<TModel> adapterProjection,
        IMapper mapper,
        IObjectsComparer objectsComparer,
        IEventSequence eventLog,
        IEventSequence eventOutbox,
        ICausationManager causationManager)
    {
        Adapter = adapter;
        Projection = adapterProjection;
        Mapper = mapper;
        _objectsComparer = objectsComparer;
        _importContexts = new();
        Adapter.DefineImport(new ImportBuilderFor<TModel, TExternalModel>(_importContexts));
        _eventLog = eventLog;
        _eventOutbox = eventOutbox;
        _causationManager = causationManager;
    }

    /// <inheritdoc/>
    public async Task Apply(TExternalModel instance)
    {
        var keyValue = Adapter.KeyResolver(instance)!;

        _causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { "AdapterId", Adapter.Identifier.ToString() },
            { "AdapterType", Adapter.GetType().AssemblyQualifiedName! },
            { "Key", keyValue.ToString()! }
        });

        var eventSourceId = keyValue;
        eventSourceId ??= new(keyValue.ToString()!);
        var initialProjectionResult = await Projection.GetById(eventSourceId!);
        var mappedInstance = Mapper.Map<TModel>(instance)!;
        var changeset = new Changeset<TModel, TModel>(_objectsComparer, mappedInstance, initialProjectionResult.Model);

        if (!_objectsComparer.Equals(initialProjectionResult.Model, mappedInstance, out var differences))
        {
            changeset.Add(new PropertiesChanged<TModel>(mappedInstance, differences));
        }

        var context = new ImportContext<TModel, TExternalModel>(initialProjectionResult, changeset, new EventsToAppend());
        _importContexts.OnNext(context);

        if (!context.Events.Any()) return;

        var eventsToAppend = context.Events.Select(_ => new EventAndValidFrom(_.Event, _.ValidFrom)).ToArray();
        await _eventLog.AppendMany(eventSourceId!, eventsToAppend);

        var publicEventsToAppend = eventsToAppend.Where(_ => _.Event.GetType().GetCustomAttribute<EventTypeAttribute>()?.IsPublic ?? false).ToArray();

        if (publicEventsToAppend.Length == 0) return;
        await _eventOutbox.AppendMany(eventSourceId!, publicEventsToAppend);
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
