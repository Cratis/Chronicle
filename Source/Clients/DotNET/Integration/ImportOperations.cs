// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using AutoMapper;
using Cratis.Auditing;
using Cratis.Changes;
using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImportOperations{TModel, TExternalModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model the operations are for.</typeparam>
/// <typeparam name="TExternalModel">Type of external model the operations are for.</typeparam>
public class ImportOperations<TModel, TExternalModel> : IImportOperations<TModel, TExternalModel>
{
    /// <summary>
    /// The causation adapter id property.
    /// </summary>
    public const string CausationAdapterIdProperty = "adapterId";

    /// <summary>
    /// The causation adapter type property.
    /// </summary>
    public const string CausationAdapterTypeProperty = "adapterType";

    /// <summary>
    /// The causation key property.
    /// </summary>
    public const string CausationKeyProperty = "Key";

    /// <summary>
    /// The causation type for client observer.
    /// </summary>
    public static readonly CausationType CausationType = new("Import Operation");

    readonly Subject<ImportContext<TModel, TExternalModel>> _importContexts;
    readonly IObjectComparer _objectComparer;
    readonly IEventSequence _eventLog;
    readonly ICausationManager _causationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportOperations{TModel, TExternalModel}"/> class.
    /// </summary>
    /// <param name="adapter">The <see cref="IAdapterFor{TModel, TExternalModel}"/>.</param>
    /// <param name="adapterProjection">The <see cref="IAdapterProjectionFor{TModel}"/> for the model.</param>
    /// <param name="mapper"><see cref="IMapper"/> to use for mapping between external model and model.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> to compare objects with.</param>
    /// <param name="eventLog">The <see cref="IEventSequence"/> for appending private events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    public ImportOperations(
        IAdapterFor<TModel, TExternalModel> adapter,
        IAdapterProjectionFor<TModel> adapterProjection,
        IMapper mapper,
        IObjectComparer objectComparer,
        IEventSequence eventLog,
        ICausationManager causationManager)
    {
        Adapter = adapter;
        Projection = adapterProjection;
        Mapper = mapper;
        _objectComparer = objectComparer;
        _importContexts = new();
        Adapter.DefineImport(new ImportBuilderFor<TModel, TExternalModel>(_importContexts));
        _eventLog = eventLog;
        _causationManager = causationManager;
    }

    /// <inheritdoc/>
    public IAdapterFor<TModel, TExternalModel> Adapter { get; }

    /// <inheritdoc/>
    public IAdapterProjectionFor<TModel> Projection { get; }

    /// <inheritdoc/>
    public IMapper Mapper { get; }

    /// <inheritdoc/>
    public async Task Apply(TExternalModel instance)
    {
        var keyValue = Adapter.KeyResolver(instance)!;

        _causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationAdapterIdProperty, Adapter.Identifier.ToString() },
            { CausationAdapterTypeProperty, Adapter.GetType().AssemblyQualifiedName! },
            { CausationKeyProperty, keyValue.ToString()! }
        });

        var eventSourceId = keyValue;
        eventSourceId ??= new(keyValue.ToString()!);
        var initialProjectionResult = await Projection.GetById(eventSourceId!);
        var mappedInstance = Mapper.Map<TModel>(instance)!;
        var changeset = new Changeset<TModel, TModel>(_objectComparer, mappedInstance, initialProjectionResult.Model);

        if (!_objectComparer.Equals(initialProjectionResult.Model, mappedInstance, out var differences))
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
