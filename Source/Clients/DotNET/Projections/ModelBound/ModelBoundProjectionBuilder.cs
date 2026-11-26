// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Builds projection definitions from model-bound attributes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModelBoundProjectionBuilder"/> class.
/// </remarks>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="currentEventStoreName">
/// The name of the event store this builder is targeting.
/// Used to detect when event types belong to the same store, which means event-log rather than inbox routing.
/// </param>
internal class ModelBoundProjectionBuilder(
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    string? currentEventStoreName = null) : IModelBoundProjectionBuilder
{
    readonly INamingPolicy _namingPolicy = namingPolicy;
    readonly IEventTypes _eventTypes = eventTypes;
    readonly string? _currentEventStoreName = currentEventStoreName;
    readonly Dictionary<string, EventType> _eventTypeCache = new();
    ProjectedMemberProcessor? _memberProcessor;

    ProjectedMemberProcessor MemberProcessor => _memberProcessor ??= new(GetOrCreateEventType, _namingPolicy, ProcessMember);

    /// <summary>
    /// Builds a projection definition from a type with model-bound projection attributes.
    /// </summary>
    /// <param name="modelType">The type of the read model.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    public ProjectionDefinition Build(Type modelType)
    {
        var projectionId = ArtifactIdentity.ModelProjectionIdFor(modelType);
        var readModelIdentifier = modelType.GetReadModelIdentifier();
        var fromEventSequenceAttr = modelType.GetCustomAttribute<EventSequenceAttribute>();
        var notRewindableAttr = modelType.GetCustomAttribute<NotRewindableAttribute>();
        var definition = CreateDefinition(modelType, projectionId, readModelIdentifier, fromEventSequenceAttr, notRewindableAttr is null);

        var classLevelFromEvents = modelType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(FromEventAttribute<>))
            .ToList();

        InitializeClassLevelFromDefinitions(definition, classLevelFromEvents);
        ProcessClassLevelRemovedWith(modelType, definition);
        var primaryConstructor = ProcessRecord(modelType, definition, classLevelFromEvents);
        ProcessProperties(modelType, definition, classLevelFromEvents, primaryConstructor);

        var hasNoAutoMap = Attribute.IsDefined(modelType, typeof(NoAutoMapAttribute), inherit: true);
        definition.AutoMap = hasNoAutoMap ? (Contracts.Projections.AutoMap)AutoMap.Disabled : (Contracts.Projections.AutoMap)AutoMap.Enabled;
        definition.NoAutoMapProperties = [.. NoAutoMapProperties.CollectFrom(modelType, _namingPolicy)];

        if (fromEventSequenceAttr is null)
        {
            definition.EventSequenceId = InferEventSequenceId(modelType, definition, _currentEventStoreName);
        }

        EventStreamReadModel.ValidateProjection(modelType, definition);
        return definition;
    }

    /// <summary>
    /// Builds a projection definition for one variant of a mutually exclusive group of read models (see
    /// <see cref="VariantOfAttribute{TIdentity}"/>).
    /// </summary>
    /// <param name="modelType">The type of the variant read model.</param>
    /// <param name="siblingVariantTypes">The other variant types in the same group.</param>
    /// <param name="globalHandlerTypes">The <see cref="GlobalForAttribute{TIdentity}"/> types declaring handlers shared by every variant in the group.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>, an ordinary and independent definition for this variant alone.</returns>
    /// <remarks>
    /// The variant is built exactly as an ordinary read model, then reclassified: only its declared
    /// <see cref="EntersOnAttribute{TEvent}"/> event(s) remain create-or-update <c language="csharp">From</c>
    /// handlers. Every other event handler the variant declares is moved to an update-only, self-referential
    /// <c language="csharp">Join</c> - keyed by the variant's own <see cref="KeyAttribute"/> property matched
    /// against the event source id - so it can update an already-active instance but can never create or
    /// resurrect one. Mutual exclusion is expressed by adding a <c language="csharp">RemovedWith</c> entry for
    /// every sibling variant's entering event(s).
    /// </remarks>
    public ProjectionDefinition BuildVariant(Type modelType, IEnumerable<Type> siblingVariantTypes, IEnumerable<Type>? globalHandlerTypes = null)
    {
        var definition = Build(modelType);
        MergeGlobalHandlers(modelType, definition, globalHandlerTypes ?? []);
        ReclassifyForVariant(modelType, definition);
        AddMutualExclusion(definition, siblingVariantTypes);
        return definition;
    }

    /// <summary>
    /// Adds an every-event mapping to the definition that owns the annotated member.
    /// </summary>
    /// <param name="definition">The owning every-event definition.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <param name="attribute">The mapping attribute.</param>
    internal static void AddFromEveryMapping(FromEveryDefinition definition, INamingPolicy namingPolicy, string propertyName, FromEveryAttribute attribute)
    {
        if (!string.IsNullOrEmpty(attribute.ContextProperty))
        {
            var contextPropertyName = PropertyValidator.ValidateContextPropertyPath<EventContext>(attribute.ContextProperty);
            var convertedContextPropertyName = ProjectionPropertyPaths.RenderContextPath(contextPropertyName, namingPolicy);
            definition.Properties[propertyName] = $"{WellKnownExpressions.EventContext}({convertedContextPropertyName})";
        }
        else
        {
            definition.Properties[propertyName] = string.IsNullOrEmpty(attribute.Property)
                ? propertyName
                : (string)ProjectionPropertyPaths.Resolve(attribute.Property, namingPolicy, ProjectionPathRole.Source);
        }

        definition.IncludeChildren = true;
    }

    static ProjectionDefinition CreateDefinition(Type modelType, ProjectionId projectionId, ReadModelIdentifier readModelIdentifier, EventSequenceAttribute? fromEventSequenceAttr, bool isRewindable)
    {
        return new ProjectionDefinition
        {
            EventSequenceId = fromEventSequenceAttr?.Sequence ?? EventSequenceId.Log,
            Identifier = projectionId,
            ReadModel = readModelIdentifier,
            IsActive = !modelType.IsPassive(),
            IsRewindable = isRewindable,
            InitialModelState = "{}",
            From = new Dictionary<EventType, FromDefinition>(),
            Join = new Dictionary<EventType, JoinDefinition>(),
            Children = new Dictionary<string, ChildrenDefinition>(),
            Nested = new Dictionary<string, ChildrenDefinition>(),
            All = new FromEveryDefinition(),
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
            RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Tags = modelType.GetTags().ToArray()
        };
    }

    static ConstructorInfo? GetPrimaryConstructor(Type modelType) =>
        modelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

    /// <summary>
    /// Merges the mappings declared by shared handlers into this variant's definition, before reclassification
    /// turns everything that is not an entering event into an update-only join.
    /// </summary>
    /// <param name="modelType">The variant being built.</param>
    /// <param name="definition">The variant's definition so far.</param>
    /// <param name="globalHandlerTypes">The shared handler types for the variant's identity.</param>
    /// <exception cref="GlobalHandlerPropertyNotOnVariant">Thrown when a shared mapping targets a member the variant does not have.</exception>
    void MergeGlobalHandlers(Type modelType, ProjectionDefinition definition, IEnumerable<Type> globalHandlerTypes)
    {
        var memberNames = GetMemberNames(modelType);

        var sharedMappings = globalHandlerTypes.SelectMany(globalHandlerType => Build(globalHandlerType).From
            .SelectMany(globalFrom => globalFrom.Value.Properties
                .Select(mapping => (HandlerType: globalHandlerType, EventType: globalFrom.Key, PropertyName: mapping.Key, Expression: mapping.Value))));

        foreach (var (handlerType, eventType, propertyName, expression) in sharedMappings)
        {
            if (!memberNames.Contains(propertyName))
            {
                throw new GlobalHandlerPropertyNotOnVariant(handlerType, modelType, propertyName);
            }

            definition.From.GetOrCreateFromDefinition(eventType).Properties[propertyName] = expression;
        }
    }

    HashSet<string> GetMemberNames(Type modelType)
    {
        var names = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => (string)ProjectionPropertyPaths.Resolve(property.Name, _namingPolicy, ProjectionPathRole.Target));

        return new HashSet<string>(names, StringComparer.Ordinal);
    }

    void ReclassifyForVariant(Type modelType, ProjectionDefinition definition)
    {
        var entersOnAttributes = modelType.GetAttributesOfGenericType<EntersOnAttribute<object>>().ToList();
        if (entersOnAttributes.Count == 0)
        {
            throw new VariantMustDeclareEntersOnEvent(modelType);
        }

        var entersOnEventTypeIds = new HashSet<EventType>();
        foreach (var (attr, eventType) in entersOnAttributes)
        {
            var eventTypeId = GetOrCreateEventType(eventType);
            entersOnEventTypeIds.Add(eventTypeId);
            ApplyEntersOnKey(definition, eventTypeId, attr);
        }

        VariantReclassifier.Reclassify(definition.From, definition.Join, entersOnEventTypeIds, GetOwnKeyPropertyName(modelType));
    }

    void ApplyEntersOnKey(ProjectionDefinition definition, EventType eventTypeId, Attribute entersOn)
    {
        var keyProperty = entersOn.GetType().GetProperty(nameof(EntersOnAttribute<object>.Key));
        var key = keyProperty?.GetValue(entersOn) as string ?? WellKnownExpressions.EventSourceId;
        var convertedKey = key.StartsWith('$') ? key : (string)ProjectionPropertyPaths.Resolve(key, _namingPolicy, ProjectionPathRole.Source);

        if (!definition.From.TryGetValue(eventTypeId, out var fromDefinition))
        {
            fromDefinition = new FromDefinition { Properties = new Dictionary<string, string>() };
            definition.From[eventTypeId] = fromDefinition;
        }

        fromDefinition.Key = convertedKey;
    }

    void AddMutualExclusion(ProjectionDefinition definition, IEnumerable<Type> siblingVariantTypes)
    {
        var siblingEnteringEventTypes = siblingVariantTypes
            .SelectMany(siblingType => siblingType.GetAttributesOfGenericType<EntersOnAttribute<object>>())
            .Select(sibling => GetOrCreateEventType(sibling.EventType));

        VariantReclassifier.AddMutualExclusion(definition.RemovedWith, siblingEnteringEventTypes);
    }

    string GetOwnKeyPropertyName(Type modelType)
    {
        var keyParameter = GetPrimaryConstructor(modelType)?.GetParameters().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>(true) is not null);
        if (keyParameter is not null)
        {
            return ProjectionPropertyPaths.Resolve(keyParameter.Name!, _namingPolicy, ProjectionPathRole.Target);
        }

        var keyProperty = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute), true));
        if (keyProperty is not null)
        {
            return ProjectionPropertyPaths.Resolve(keyProperty.Name, _namingPolicy, ProjectionPathRole.Target);
        }

        throw new VariantMustDeclareKeyProperty(modelType);
    }

    EventSequenceId InferEventSequenceId(Type modelType, ProjectionDefinition definition, string? currentEventStoreName)
    {
        // Model-bound projections have always selected source ownership from the latest registered generation,
        // matching the single owner sent on the wire. Keep that source-selection contract while resolving facets through the registry.
        var eventStores = definition.From.Keys
            .Where(et => _eventTypes.HasFor(new EventTypeId(et.Id)))
            .Select(et => _eventTypes.GetClrTypeFor(new EventTypeId(et.Id)))
            .Select(type => _eventTypes.GetDescriptorFor(type, currentEventStoreName is null ? EventStoreName.NotSet : new(currentEventStoreName)).EventStore)
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct()
            .ToList();

        if (eventStores.Count > 1)
        {
            throw new MultipleEventStoresDefined(modelType, eventStores);
        }

        if (eventStores.Count == 1)
        {
            // If the event types belong to the same store as the one we're in, use event-log instead of inbox
            if (currentEventStoreName is not null && eventStores[0] == currentEventStoreName)
            {
                return EventSequenceId.Log;
            }

            return new EventSequenceId($"{EventSequenceId.InboxPrefix}{eventStores[0]}");
        }

        return EventSequenceId.Log;
    }

    EventType GetOrCreateEventType(Type eventType)
    {
        var chronicleEventType = _eventTypes.GetEventTypeFor(eventType);
        var key = $"{chronicleEventType.Id}_{chronicleEventType.Generation}_{chronicleEventType.Tombstone}";

        if (!_eventTypeCache.TryGetValue(key, out var cachedEventType))
        {
            cachedEventType = chronicleEventType.ToContract();
            _eventTypeCache[key] = cachedEventType;
        }

        return cachedEventType;
    }

    void InitializeClassLevelFromDefinitions(ProjectionDefinition definition, List<Attribute> classLevelFromEvents)
    {
        // Initialize From definitions for class-level FromEvent attributes.
        // This ensures that even if NoAutoMapAttribute prevents auto-mapping,
        // we still have the From definition structure.
        foreach (var attr in classLevelFromEvents)
        {
            var eventTypeId = GetOrCreateEventType(attr.GetType().GetGenericArguments()[0]);
            if (!definition.From.ContainsKey(eventTypeId))
            {
                definition.From[eventTypeId] = new FromDefinition
                {
                    Key = WellKnownExpressions.EventSourceId,
                    Properties = new Dictionary<string, string>()
                };
            }
        }
    }

    void ProcessClassLevelRemovedWith(Type modelType, ProjectionDefinition definition)
    {
        foreach (var (attr, eventType) in modelType.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            definition.RemovedWith.ProcessRemovedWithAttribute(GetOrCreateEventType, _namingPolicy, attr, eventType);
        }

        foreach (var (attr, eventType) in modelType.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            definition.RemovedWithJoin.ProcessRemovedWithJoinAttribute(GetOrCreateEventType, _namingPolicy, attr, eventType);
        }
    }

    void ProcessProperties(Type modelType, ProjectionDefinition definition, List<Attribute> classLevelFromEvents, ConstructorInfo? primaryConstructor)
    {
        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var isConstructorParameter = primaryConstructor?.GetParameters()
                .Any(p => p.Name?.Equals(property.Name, StringComparison.OrdinalIgnoreCase) == true) == true;

            // A positional record's properties are also constructor parameters, already handled by
            // ProcessRecord. But an attribute applied to the generated PROPERTY via `[property:]` — the
            // only way to place a property-only attribute such as [FromAll] on a positional parameter — is
            // not visible on the parameter, so it must still be processed here. Parameter-targeted and
            // property-targeted attributes are disjoint, so this does not double-process. Class-level
            // FromEvent wiring was already applied by the parameter pass, so pass none for those members.
            ProcessMember(
                property,
                definition,
                isConstructorParameter ? [] : classLevelFromEvents,
                isRoot: true,
                modelType: modelType);
        }
    }

    ConstructorInfo? ProcessRecord(Type modelType, ProjectionDefinition definition, List<Attribute> classLevelFromEvents)
    {
        var primaryConstructor = GetPrimaryConstructor(modelType);

        foreach (var parameter in primaryConstructor?.GetParameters() ?? [])
        {
            MemberProcessor.Process(new MemberProcessingScope(new ProjectedMember(parameter), definition, classLevelFromEvents, true, modelType, null));
        }

        return primaryConstructor;
    }

    void ProcessMember(
        MemberInfo property,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents,
        bool isRoot,
        Type? modelType = null,
        ChildrenDefinition? childrenDef = null) =>
        MemberProcessor.Process(new MemberProcessingScope(new ProjectedMember(property), definition, classLevelFromEvents, isRoot, modelType, childrenDef));
}
