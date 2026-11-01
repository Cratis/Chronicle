// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.DependencyInjection;
using Cratis.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents an implementation of <see cref="IProjectionFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionFactory"/> class.
/// </remarks>
/// <param name="propertyMapperExpressionResolvers"><see cref="IReadModelPropertyExpressionResolvers"/> for resolving expressions for properties.</param>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving expressions for accessing values on events.</param>
/// <param name="keyExpressionResolvers"><see cref="IKeyExpressionResolvers"/> for resolving keys.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from expando objects.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers"/> for resolving <see cref="Key"/>.</param>
/// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
/// <param name="logger">The logger.</param>
/// <param name="eventCompliance">Optional <see cref="IEventCompliance"/> for releasing stored join events before projecting them.</param>
/// <param name="eventGenerationRelease">The pinned-generation release boundary for stored key and join events.</param>
[Singleton]
public class ProjectionFactory(
    IReadModelPropertyExpressionResolvers propertyMapperExpressionResolvers,
    IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers,
    IKeyExpressionResolvers keyExpressionResolvers,
    IExpandoObjectConverter expandoObjectConverter,
    IKeyResolvers keyResolvers,
    IStorage storage,
    ILogger<ProjectionFactory> logger,
    IEventCompliance? eventCompliance = null,
    IEventGenerationRelease? eventGenerationRelease = null) : IProjectionFactory
{
    /// <summary>
    /// Gets the serializer options that write a definition in full, with dictionaries in a form that does not depend on enumeration order.
    /// </summary>
    internal static readonly JsonSerializerOptions FingerprintOptions = new()
    {
        Converters =
        {
            new ConceptAsJsonConverterFactory(),
            new PropertyPathJsonConverter(),
            new PropertyPathChildrenDefinitionDictionaryJsonConverter(),
            new PropertyExpressionDictionaryConverter(),
            new FingerprintEventDefinitionsConverter<FromDefinition>(),
            new FingerprintEventDefinitionsConverter<JoinDefinition>(),
            new FingerprintEventDefinitionsConverter<RemovedWithDefinition>(),
            new FingerprintEventDefinitionsConverter<RemovedWithJoinDefinition>()
        }
    };

    static readonly string[] _aggregateExpressions =
    [
        WellKnownExpressions.Count,
        WellKnownExpressions.Increment,
        WellKnownExpressions.Decrement,
        WellKnownExpressions.Add,
        WellKnownExpressions.Subtract
    ];

    /// <inheritdoc/>
    public async Task<IProjection> Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionDefinition definition, ReadModelDefinition readModelDefinition, IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        if (readModelDefinition?.Sink is null)
        {
            throw new ReadModels.ReadModelNotFound(definition.ReadModel);
        }

        readModelDefinition.Sink.EnsureReadModelSupported();
        ProjectionGenerationDeliveryValidation.Validate(definition);
        if (definition.GenerationDelivery == EventGenerationDelivery.Pinned &&
            (eventGenerationRelease is null || definition.SubscribesToAllEvents))
        {
            throw new EventGenerationDeliveryNotSupported();
        }

        // The fingerprint identifies the stored definition; ProjectionsManager compares it against the stored definition.
        var fingerprint = DefinitionFingerprintFor(definition);
        var schemas = eventTypeSchemas as IReadOnlyList<EventTypeSchema> ?? [.. eventTypeSchemas];
        var rewrittenEventTypes = new List<EventType>();
        definition = LegacyEventSourceIdKeys.Normalize(definition, schemas, rewrittenEventTypes);
        foreach (var eventType in rewrittenEventTypes.Distinct())
        {
            LegacyEventSourceIdKeys.LogRewritten(logger, definition.Identifier.Value, eventType.Id.Value);
        }

        var normalizedDefinition = LegacyChildEveryEventMappings.Normalize(definition, readModelDefinition.GetTargetSchema());
        foreach (var property in definition.FromEvery.Properties.Keys.Except(normalizedDefinition.FromEvery.Properties.Keys))
        {
            LegacyChildEveryEventMappings.LogMoved(logger, definition.Identifier.Value, property.Path);
        }
        definition = normalizedDefinition;

        var eventSequenceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(definition.EventSequenceId);

        // Only a join that releases a stored event needs the schema of the generation it was stored at.
        var eventTypesStorage = eventCompliance is null && definition.GenerationDelivery != EventGenerationDelivery.Pinned ? null : storage.GetEventStore(eventStore).EventTypes;
        var projection = (Projection)await CreateProjectionFrom(
            eventStore,
            new(eventSequenceStorage, eventTypesStorage, schemas, readModelDefinition),
            new(definition, readModelDefinition.GetTargetSchema(), PropertyPath.Root, PropertyPath.Root),
            ProjectionPath.GetRootFor(definition.Identifier),
            false);
        WarnUnsupportedRootJoinKey(projection, definition);
        projection.DefinitionFingerprint = fingerprint;
        return projection;
    }

    /// <summary>
    /// Computes a stable fingerprint of pipeline-affecting rules, independent of dictionary ordering and registration metadata.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <returns>The SHA-256 fingerprint.</returns>
    internal static string DefinitionFingerprintFor(ProjectionDefinition definition)
    {
        // Comparison discards transport metadata. Keep the model identity and initial state here:
        // unlike replay recommendations, pipeline reuse depends on both of them.
        var normalized = ProjectionDefinitionNormalization.ForComparison(definition, false) with
        {
            ReadModel = definition.ReadModel,
            InitialModelState = definition.InitialModelState
        };
        var canonical = Canonicalize(JsonSerializer.SerializeToNode(normalized, FingerprintOptions));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical!.ToJsonString())));
    }

    /// <summary>
    /// Finds collection (array) read-model properties that AutoMap to nothing — a property Chronicle expects to
    /// fill by matching an event property of the same name, but for which no source event carries a matching
    /// property and no explicit mapping exists. Such a property silently projects as an empty collection.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> for the projection level being inspected.</param>
    /// <param name="currentReadModelSchema">The <see cref="JsonSchema"/> for the read model (or child) at this level.</param>
    /// <param name="autoMap">The effective <see cref="AutoMap"/> setting for this projection level.</param>
    /// <param name="noAutoMapProperties">Read model properties explicitly opted out of AutoMap.</param>
    /// <param name="eventTypeSchemas">The <see cref="EventTypeSchema">schemas</see> for the participating event types.</param>
    /// <returns>The collection properties that will always project empty; empty when everything maps.</returns>
    internal static IReadOnlyList<UnmappedCollectionProperty> FindUnmappedCollectionProperties(
        ProjectionDefinition projectionDefinition,
        JsonSchema currentReadModelSchema,
        AutoMap autoMap,
        IReadOnlySet<string> noAutoMapProperties,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        // The silent-empty failure only happens when AutoMap is expected to wire the property by name. With
        // AutoMap disabled the developer has taken over mapping, so an unmapped collection is intentional.
        if (autoMap != AutoMap.Enabled || currentReadModelSchema is null)
        {
            return [];
        }

        if (projectionDefinition.From.Count == 0)
        {
            return [];
        }

        var schemaList = eventTypeSchemas as IReadOnlyList<EventTypeSchema> ?? eventTypeSchemas.ToList();
        var covered = CollectCoveredPropertyNames(projectionDefinition, currentReadModelSchema, schemaList, new AutoMapSettings(autoMap, noAutoMapProperties));

        // Child and nested collections are materialized as their own projection structures, not by name-AutoMap.
        var structural = new HashSet<string>(
            projectionDefinition.Children.Keys.Concat(projectionDefinition.Nested?.Keys ?? []).Select(key => key.LastSegment.Value),
            StringComparer.OrdinalIgnoreCase);

        var eventTypeNames = string.Join(", ", projectionDefinition.From.Keys.Select(_ => _.Id.Value));

        return currentReadModelSchema.Properties.Values
            .Where(property => property.IsArray &&
                               !covered.Contains(property.Name) &&
                               !structural.Contains(property.Name) &&
                               !noAutoMapProperties.Contains(property.Name))
            .Select(property => new UnmappedCollectionProperty(property.Name, eventTypeNames))
            .ToList();
    }

    /// <summary>Combines explicit join mappings with matching event properties when AutoMap permits them.</summary>
    /// <param name="joinDefinition">The event-specific join definition.</param>
    /// <param name="currentReadModelSchema">The schema being projected into.</param>
    /// <param name="eventSchema">The joined event's schema, if available.</param>
    /// <param name="autoMap">The inherited automatic-mapping setting.</param>
    /// <param name="noAutoMapProperties">Properties excluded from automatic mapping.</param>
    /// <returns>The effective join property mappings.</returns>
    internal static List<KeyValuePair<PropertyPath, string>> GetMergedJoinProperties(JoinDefinition joinDefinition, JsonSchema currentReadModelSchema, JsonSchema? eventSchema, AutoMap autoMap, IReadOnlySet<string> noAutoMapProperties)
    {
        var merged = joinDefinition.Properties.ToList();
        autoMap = joinDefinition.AutoMap == AutoMap.Inherit ? autoMap : joinDefinition.AutoMap;

        if (autoMap == AutoMap.Disabled)
        {
            return merged;
        }

        if (eventSchema is null || currentReadModelSchema is null)
        {
            return merged;
        }

        var existingReadModelProperties = new HashSet<string>(merged.Select(_ => _.Key.LastSegment.Value), StringComparer.OrdinalIgnoreCase);
        var existingEventProperties = new HashSet<string>(merged.Select(_ => _.Value), StringComparer.OrdinalIgnoreCase);

        foreach (var eventProperty in eventSchema.Properties.Values.Where(property => !existingReadModelProperties.Contains(property.Name) && !existingEventProperties.Contains(property.Name)))
        {
            var matchingReadModelProperty = FindAutoMapTarget(eventProperty, currentReadModelSchema, noAutoMapProperties);
            if (matchingReadModelProperty is not null)
            {
                merged.Add(new(new PropertyPath(matchingReadModelProperty.Name), eventProperty.Name));
                existingReadModelProperties.Add(matchingReadModelProperty.Name);
                existingEventProperties.Add(eventProperty.Name);
            }
        }

        return merged;
    }

    static JsonNode? Canonicalize(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => KeyValuePair.Create(pair.Key, Canonicalize(pair.Value)))),
        JsonArray array => new JsonArray(array.Select(Canonicalize).ToArray()),
        null => null,
        _ => node.DeepClone()
    };

    /// <summary>
    /// Finds the read model property an event property auto-maps onto. A property flagged with [NoAutoMap] is only
    /// ever set from its explicit mapping, so an event carrying an identically named property must not auto-map onto it.
    /// </summary>
    /// <param name="eventProperty">The event property to find a target for.</param>
    /// <param name="currentReadModelSchema">The schema being projected into.</param>
    /// <param name="noAutoMapProperties">Properties excluded from automatic mapping.</param>
    /// <returns>The matching read model property, or null when there is none or it is excluded.</returns>
    static JsonSchemaProperty? FindAutoMapTarget(JsonSchemaProperty eventProperty, JsonSchema currentReadModelSchema, IReadOnlySet<string> noAutoMapProperties)
    {
        var matchingReadModelProperty = currentReadModelSchema.Properties.Values
            .FirstOrDefault(rmp => rmp.Name.Equals(eventProperty.Name, StringComparison.OrdinalIgnoreCase));

        return matchingReadModelProperty is not null && !noAutoMapProperties.Contains(matchingReadModelProperty.Name) ? matchingReadModelProperty : null;
    }

    static HashSet<string> CollectCoveredPropertyNames(
        ProjectionDefinition projectionDefinition,
        JsonSchema currentReadModelSchema,
        IReadOnlyList<EventTypeSchema> schemaList,
        AutoMapSettings autoMap)
    {
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void CoverFrom(FromDefinition fromDefinition, EventType eventType)
        {
            var eventSchema = schemaList.SchemaFor(eventType, projectionDefinition.GenerationDelivery)?.Schema;
            foreach (var mapping in GetMergedFromProperties(fromDefinition, currentReadModelSchema, eventSchema, autoMap.AutoMap, autoMap.NoAutoMapProperties))
            {
                covered.Add(mapping.Key.LastSegment.Value);
            }
        }

        foreach (var (eventType, fromDefinition) in projectionDefinition.From)
        {
            CoverFrom(fromDefinition, eventType);
        }

        foreach (var (eventType, joinDefinition) in projectionDefinition.Join)
        {
            var eventSchema = schemaList.SchemaFor(eventType, projectionDefinition.GenerationDelivery)?.Schema;
            foreach (var mapping in GetMergedJoinProperties(joinDefinition, currentReadModelSchema, eventSchema, autoMap.AutoMap, autoMap.NoAutoMapProperties))
            {
                covered.Add(mapping.Key.LastSegment.Value);
            }
        }

        foreach (var mapping in projectionDefinition.FromEvery.Properties)
        {
            covered.Add(mapping.Key.LastSegment.Value);
        }

        var derivedFroms = (projectionDefinition.FromDerivatives ?? []).SelectMany(derivative => derivative.EventTypes.Select(eventType => (EventType: eventType, derivative.From)));
        foreach (var (eventType, fromDefinition) in derivedFroms)
        {
            CoverFrom(fromDefinition, eventType);
        }

        return covered;
    }

    /// <summary>
    /// Determines whether an event is subscribed only to be aggregated — every property it maps is a
    /// <c language="csharp">[Count]</c>/<c language="csharp">[Increment]</c>/<c language="csharp">[Decrement]</c>/<c language="csharp">[Add]</c>/<c language="csharp">[Subtract]</c> operation and
    /// nothing else. The developer pulled the event in to aggregate it, not to copy values off it, so its
    /// other properties must not be name-AutoMapped onto the read model.
    /// </summary>
    /// <param name="fromDefinition">The <see cref="FromDefinition"/> to inspect.</param>
    /// <returns><see langword="true"/> if the event is subscribed only for an aggregate; otherwise <see langword="false"/>.</returns>
    static bool IsAggregateOnly(FromDefinition fromDefinition) =>
        fromDefinition.Properties.Count > 0 &&
        fromDefinition.Properties.Values.All(expression =>
            _aggregateExpressions.Any(aggregate => expression.StartsWith(aggregate, StringComparison.Ordinal)));

    static List<KeyValuePair<PropertyPath, string>> GetMergedFromProperties(FromDefinition fromDefinition, JsonSchema currentReadModelSchema, JsonSchema? eventSchema, AutoMap autoMap, IReadOnlySet<string> noAutoMapProperties)
    {
        var merged = fromDefinition.Properties.ToList();

        if (autoMap == AutoMap.Disabled)
        {
            return merged;
        }

        if (eventSchema is null || currentReadModelSchema is null)
        {
            return merged;
        }

        // An event subscribed only to be aggregated does not name-AutoMap its other properties, so an
        // unrelated same-named property cannot silently overwrite an explicitly sourced one. The developer
        // can still opt a specific property out of AutoMap for the non-aggregate cases with [NoAutoMap].
        if (IsAggregateOnly(fromDefinition))
        {
            return merged;
        }

        var existing = new HashSet<string>(merged.Select(_ => _.Key.LastSegment.Value), StringComparer.OrdinalIgnoreCase);

        foreach (var eventProperty in eventSchema.Properties.Values.Where(property => !existing.Contains(property.Name)))
        {
            var matchingReadModelProperty = FindAutoMapTarget(eventProperty, currentReadModelSchema, noAutoMapProperties);
            if (matchingReadModelProperty is not null)
            {
                merged.Add(new(new PropertyPath(matchingReadModelProperty.Name), eventProperty.Name));
                existing.Add(matchingReadModelProperty.Name);
            }
        }

        return merged;
    }

    static void SetParentOnAllChildProjections(Projection projection, IProjection[] childProjections)
    {
        foreach (var child in childProjections)
        {
            child.SetParent(projection);
        }
    }

    static JsonSchemaProperty GetChildCollectionProperty(ProjectionId projectionId, ReadModelDefinition rootReadModel, JsonSchema currentReadModelSchema, PropertyPath childrenProperty)
    {
        // Resolve against the current child level, using the same casing compatibility as other schema paths.
        if (!currentReadModelSchema.Properties.TryGetValue(childrenProperty.LastSegment.Value, out var schemaProperty))
        {
            schemaProperty = currentReadModelSchema.GetSchemaPropertyForPropertyPath(new PropertyPath(childrenProperty.LastSegment.Value));
        }

        if (schemaProperty is null)
        {
            throw new MissingChildCollectionInReadModelSchema(
                projectionId,
                childrenProperty.Path,
                rootReadModel.Identifier,
                currentReadModelSchema.Properties.Keys);
        }

        return schemaProperty;
    }

    /// <summary>
    /// Answers whether a key expression is a constant rather than something read off the event.
    /// </summary>
    /// <param name="key">The <see cref="PropertyExpression"/> to consider.</param>
    /// <returns>True when the expression is a constant.</returns>
    /// <remarks>
    /// Asked of a parent key, because a constant names the parent document outright and therefore has no
    /// parent event behind it. Uses the same recognition that resolves the expression, so the two cannot
    /// drift apart.
    /// </remarks>
    static bool IsConstantExpression(PropertyExpression? key) =>
        key is not null && key.Value.Length != 0 && new ValueExpressionResolver().CanResolve(key.Value);

    static bool KeyConversionPreservesIdentity(IProjection projection, PropertyPath identifiedByProperty)
    {
        var schemaProperty = projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(identifiedByProperty);

        // Missing properties default to string in EventValueKeyExpressionResolver. Only a plain string
        // without a format or schema composition is guaranteed not to normalize distinct source ids.
        return schemaProperty is null ||
            (schemaProperty.Type == JsonObjectType.String &&
             string.IsNullOrEmpty(schemaProperty.Format) &&
             !schemaProperty.HasReference &&
             schemaProperty.AllOf.Count == 0 &&
             schemaProperty.AnyOf.Count == 0 &&
             schemaProperty.OneOf.Count == 0);
    }

    static ExpandoObject GetInitialState(IExpandoObjectConverter expandoObjectConverter, ProjectionDefinition projectionDefinition, JsonSchema readModelSchema) =>
        projectionDefinition.InitialModelState.Count == 0 ?
            CreateInitialState(readModelSchema) :
            expandoObjectConverter.ToExpandoObject(projectionDefinition.InitialModelState, readModelSchema);

    static ExpandoObject CreateInitialState(JsonSchema readModelSchema)
    {
        var initialState = new ExpandoObject();
        var stateDict = (IDictionary<string, object?>)initialState;

        foreach (var collection in readModelSchema.Properties.Values.Where(_ => _.IsArray))
        {
            stateDict[collection.Name] = new List<object>();
        }

        return initialState;
    }

    static void SetupRemovedWith(ProjectionLevel level)
    {
        var projection = level.Projection;
        foreach (var (eventType, _) in level.Definition.RemovedWith)
        {
            var observable = projection.Event
                    .WhereEventTypeEquals(eventType, projection.GenerationDelivery);
            if (level.IsChild)
            {
                projection.Subscriptions.Add(
                    observable.RemoveChild(
                        level.AccessorPath,
                        level.IdentifiedBy).Subscribe());
            }
            else
            {
                projection.Subscriptions.Add(observable.Remove().Subscribe());
            }
        }
    }

    static void ProjectJoined(ProjectionLevel level, IObservable<ProjectionEventContext> joinObservable, IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers)
    {
        var projection = level.Projection;
        if (level.IsNested)
        {
            projection.Subscriptions.Add(joinObservable.ProjectNested(propertyMappers).Subscribe());
        }
        else
        {
            joinObservable.Project(level.AccessorPath, level.IdentifiedBy, propertyMappers, subscriptions: projection.Subscriptions);
        }
    }

    static KeyValuePair<EventType, JoinDefinition>[] FindJoinExpressions(ProjectionLevel level, SubscriptionContext context, FromRegistration registration)
    {
        // A join's `on` column can be populated by AutoMap (a name-matched foreign key) rather than an explicit From
        // mapping, so match against the AutoMap-merged From properties — the same set SetupFromDefinition projects with —
        // not the raw fromDefinition.Properties. Otherwise a join on an AutoMapped column never gets its row-creation-time
        // backfill (ResolveJoin) wired, and only backfills when the join-source event arrives later — leaving the joined
        // values empty whenever the join source already exists at the time the row is created (the common production order).
        var autoMap = level.AutoMapSettings;
        var joins = level.Definition.Join;
        var mergedFromProperties = GetMergedFromProperties(registration.Definition, level.Schema, context.EventTypeSchemas.SchemaFor(registration.EventType, level.Projection.GenerationDelivery)?.Schema, autoMap.AutoMap, autoMap.NoAutoMapProperties);
        var joinExpressions = level.IsChild && !level.IsNested
            ? joins.Where(join => join.Value.On == level.IdentifiedBy).ToArray()
            : joins.Where(join => mergedFromProperties.Exists(from => join.Value.On == from.Key)).ToArray();

        // Include join expressions that join on the id property
        var currentReadModelSchema = level.Schema;
        var keyPropertyName = currentReadModelSchema.HasKeyProperty() ? currentReadModelSchema.GetKeyProperty().Name : currentReadModelSchema.GetLikelyKeyPropertyName();
        if (keyPropertyName is not null)
        {
            joinExpressions = [.. joinExpressions, .. joins.Where(join => join.Value.On == keyPropertyName)];
        }

        return joinExpressions;
    }

    static JsonSchemaProperty ResolveStaticSchemaProperty(Projection projection, PropertyPath propertyPath)
    {
        var schemaProperty = projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(propertyPath);
        if (propertyPath.LastSegment is ThisAccessor)
        {
            return new JsonSchemaProperty
            {
                Type = projection.TargetReadModelSchema.Type,
                Format = projection.TargetReadModelSchema.Format
            };
        }

        schemaProperty ??= projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(new PropertyPath(propertyPath.LastSegment.Value));

        return schemaProperty ?? new JsonSchemaProperty
        {
            Type = JsonObjectType.None
        };
    }

    static bool CanMarkAsDynamic(JsonSchema schema) =>
        schema.Type == JsonObjectType.None || (schema.Type == JsonObjectType.Object && schema.Properties.Count == 0);

    static void MarkBasePropertyAsDynamic(Projection projection, PropertyPath propertyPath, string propertyPathString)
    {
        // Extract the base property name (before .$)
        var dynamicKeyIndex = propertyPathString.IndexOf(".$", StringComparison.Ordinal);
        var basePropertyPath = propertyPathString[..dynamicKeyIndex];

        // Get or create the schema property for the base property and mark it as dynamic
        var baseSchema = projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(new PropertyPath(basePropertyPath));

        // If baseSchema is null, the property doesn't exist yet and will be created as dynamic.
        if (baseSchema is null)
        {
            return;
        }

        var actualSchema = baseSchema.ActualTypeSchema;
        if (actualSchema.IsDynamic())
        {
            return;
        }

        // An empty object schema is safe to mark as dynamic, as is one whose type is not yet determined.
        if (CanMarkAsDynamic(actualSchema))
        {
            actualSchema.MarkAsDynamic();
            return;
        }

        // Property exists and has a concrete, non-dynamic schema - a dynamic key segment is not valid here.
        throw new DynamicPropertyPathOnNonDynamicProperty(propertyPath, basePropertyPath);
    }

    /// <summary>
    /// Gets the operations contributed by derivative and from-event-property registrations. A parent derives ChildrenAffected from its
    /// children's entries, so an event type a child handles only that way must be in the child's map.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> to inspect.</param>
    /// <returns>The event types with their operation.</returns>
    static IEnumerable<(EventType EventType, ProjectionOperationType Op)> GetDerivativeAndEventPropertyOperations(ProjectionDefinition projectionDefinition)
    {
        var derivatives = projectionDefinition.FromDerivatives?.SelectMany(_ => _.EventTypes).Select(_ => (EventType: _, Op: ProjectionOperationType.From)) ?? [];
        return projectionDefinition.FromEventProperty is null
            ? derivatives
            : derivatives.Concat([(EventType: projectionDefinition.FromEventProperty.Event, Op: ProjectionOperationType.From)]);
    }

    void SetupRemovedWithJoin(ProjectionLevel level)
    {
        var projection = level.Projection;
        if (level.AccessorPath.IsRoot)
        {
            if (level.Definition.RemovedWithJoin.Count > 0)
            {
                logger.RootRemovalViaJoinNotSupported(projection.Identifier.Value);
            }

            return;
        }

        foreach (var (eventType, _) in level.Definition.RemovedWithJoin)
        {
            projection.Subscriptions.Add(
                projection.Event
                       .WhereEventTypeEquals(eventType, projection.GenerationDelivery)
                       .RemoveChildFromAll(
                           level.AccessorPath,
                           level.IdentifiedBy).Subscribe());
        }
    }

    async Task<IProjection> CreateProjectionFrom(EventStoreName eventStore, SubscriptionContext context, ProjectionTarget target, ProjectionPath path, bool isChild)
    {
        IDictionary<EventType, EventTypeSchema> pinnedSchemas = new Dictionary<EventType, EventTypeSchema>();
        async Task<AppendedEvent> ReleaseStoredEvent(EventType pin, AppendedEvent storedEvent) =>
            (await eventGenerationRelease!.Release(eventStore, [pin], pinnedSchemas, [storedEvent])).Single();

        // Phase 1: Create the projection structure with all parent-child relationships
        var (projection, childProjections, actualIdentifiedByProperty) = await CreateProjectionStructure(
            context,
            target.Definition.Identifier,
            target,
            path,
            target.Definition.GenerationDelivery,
            target.Definition.GenerationDelivery == EventGenerationDelivery.Pinned ? ReleaseStoredEvent : null);

        // Phase 2: Resolve events for all projections now that parent relationships are established
        ResolveEventsRecursively(projection, childProjections, target.Definition, actualIdentifiedByProperty, isChild);

        if (target.Definition.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            var available = context.EventTypeSchemas.ToArray();
            foreach (var pin in projection.EventTypes.Distinct())
            {
                var schema = available.SchemaFor(pin, EventGenerationDelivery.Pinned);
                if (schema is null)
                {
                    if (!await context.EventTypesStorage!.HasFor(pin.Id, pin.Generation))
                    {
                        throw new PinnedEventTypeGenerationUnavailable(pin);
                    }

                    schema = (await context.EventTypesStorage.GetFor([pin])).SchemaFor(pin, EventGenerationDelivery.Pinned);
                }

                pinnedSchemas[pin] = schema ?? throw new PinnedEventTypeGenerationUnavailable(pin);
            }

            context = context with { EventTypeSchemas = pinnedSchemas.Values };
        }

        // Phase 3: Setup subscriptions for all projections (root and children)
        var level = new ProjectionLevel(projection, target with { IdentifiedBy = actualIdentifiedByProperty }, isChild);
        SetupSubscriptionsRecursively(level, childProjections, context);

        return projection;
    }

    void SetupSubscriptionsRecursively(ProjectionLevel level, IProjection[] childProjections, SubscriptionContext context)
    {
        // First setup subscriptions for all children (depth-first)
        foreach (var childEntry in level.Definition.Children.Zip(childProjections, (kvp, child) => (kvp, child)))
        {
            if (childEntry.child is Projection childProjection)
            {
                SetupChildSubscriptions(level, childEntry.kvp, childProjection, context);
            }
        }

        // Then setup subscriptions for the current projection
        SetupFromEventPropertyAndJoins(level, context);
    }

    void SetupChildSubscriptions(ProjectionLevel level, KeyValuePair<PropertyPath, ChildrenDefinition> childEntry, Projection childProjection, SubscriptionContext context)
    {
        var childDefinition = childEntry.Value;
        var childIdentifiedBy = childDefinition.IdentifiedBy.IsRoot ?
            childProjection.IdentifiedByProperty :
            childDefinition.IdentifiedBy;
        var childrenProperty = GetChildCollectionProperty(level.Projection.Identifier, context.RootReadModel, level.Schema, childEntry.Key);
        var childSchema = childrenProperty.Item?.ActualSchema ?? level.Schema;

        SetupSubscriptionsRecursively(
            new ProjectionLevel(childProjection, new(childDefinition, childSchema, childProjection.ChildrenPropertyPath, childIdentifiedBy), true),
            childProjection.ChildProjections.ToArray(),
            context);
    }

    void SetupNestedSubscriptions(ProjectionLevel level, SubscriptionContext context)
    {
        var nested = level.Definition.Nested;
        if (nested is null || nested.Count == 0)
        {
            return;
        }

        foreach (var kvp in nested)
        {
            SetupNestedEntry(level, context, kvp.Key, kvp.Value);
        }
    }

    void SetupNestedEntry(ProjectionLevel level, SubscriptionContext context, PropertyPath nestedKey, ChildrenDefinition nestedDefinition)
    {
        var projection = level.Projection;
        var nestedPropertyPath = level.AccessorPath.IsRoot ? nestedKey : level.AccessorPath + nestedKey;
        LogUnsupportedNestedDefinitions(projection, nestedDefinition, nestedPropertyPath);

        var nestedSchema = level.Schema.Properties.TryGetValue(nestedKey.LastSegment.Value, out var schemaProp)
            ? schemaProp.ActualSchema ?? level.Schema
            : level.Schema;
        var propertyMappersForEveryEventType = nestedDefinition.FromEvery.Properties.Select(p => ResolveEveryEventPropertyMapper(projection, nestedPropertyPath + p.Key, p.Value)).ToList();
        var everyEventMappers = new EveryEventMappers(propertyMappersForEveryEventType);

        // Resolve mappings against the scalar object's schema and its own AutoMap exclusions.
        var nestedNoAutoMapProperties = (nestedDefinition.NoAutoMapProperties ?? [])
            .Select(_ => _.LastSegment.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nestedAutoMap = nestedDefinition.AutoMap == AutoMap.Inherit ? projection.AutoMap : nestedDefinition.AutoMap;
        var nestedLevel = new ProjectionLevel(
            projection,
            new(nestedDefinition, nestedSchema, nestedPropertyPath, level.IdentifiedBy),
            level.IsChild,
            IsNested: true,
            NestedAutoMap: new(nestedAutoMap, nestedNoAutoMapProperties));

        SetupNestedFromSubscriptions(nestedLevel, context, everyEventMappers);

        if (!projection.HasParent)
        {
            SetupJoinSubscriptions(nestedLevel, context, everyEventMappers, nestedDefinition.From.Keys.Select(_ => _.Id).ToHashSet());
        }

        foreach (var (eventType, _) in nestedDefinition.RemovedWith)
        {
            projection.Subscriptions.Add(
                projection.Event
                    .WhereEventTypeEquals(eventType, projection.GenerationDelivery)
                    .ClearNested(nestedPropertyPath).Subscribe());
        }

        // Recursively set up nested objects within this nested object
        SetupNestedSubscriptions(nestedLevel, context);
    }

    void LogUnsupportedNestedDefinitions(Projection projection, ChildrenDefinition nestedDefinition, PropertyPath nestedPropertyPath)
    {
        if (nestedDefinition.Children.Count > 0)
        {
            logger.NestedChildrenNotSupported(projection.Identifier.Value, nestedPropertyPath.Path);
        }
        if (nestedDefinition.RemovedWithJoin.Count > 0)
        {
            logger.NestedRemovalViaJoinNotSupported(projection.Identifier.Value, nestedPropertyPath.Path);
        }
        if (projection.HasParent && nestedDefinition.Join.Count > 0)
        {
            logger.NestedJoinInChildrenNotSupported(projection.Identifier.Value, nestedPropertyPath.Path);
        }
    }

    void SetupNestedFromSubscriptions(ProjectionLevel level, SubscriptionContext context, EveryEventMappers everyEventMappers)
    {
        var projection = level.Projection;
        var autoMap = level.AutoMapSettings;
        foreach (var (eventType, fromDefinition) in level.Definition.From)
        {
            var matchingSchema = context.EventTypeSchemas.SchemaFor(eventType, projection.GenerationDelivery);
            var mergedProperties = GetMergedFromProperties(fromDefinition, level.Schema, matchingSchema?.Schema, autoMap.AutoMap, autoMap.NoAutoMapProperties);
            var propertyMappers = mergedProperties.ConvertAll(p => ResolvePropertyMapper(projection, level.AccessorPath + p.Key, p.Value));
            propertyMappers.AddRange(everyEventMappers.ClaimFor(eventType.Id));

            var fromObservable = projection.Event.WhereEventTypeEquals(eventType, projection.GenerationDelivery);
            projection.Subscriptions.Add(fromObservable.ProjectNested(propertyMappers).Subscribe());
            if (!projection.HasParent)
            {
                SetupJoinsForFromDefinition(level, context, new(new(fromDefinition, eventType), fromObservable));
            }
        }
    }

    async Task<(Projection Projection, IProjection[] ChildProjections, PropertyPath ActualIdentifiedByProperty)> CreateProjectionStructure(
        SubscriptionContext context,
        ProjectionId projectionId,
        ProjectionTarget target,
        ProjectionPath path,
        EventGenerationDelivery generationDelivery,
        Func<EventType, AppendedEvent, Task<AppendedEvent>>? releaseStoredEvent)
    {
        var (projectionDefinition, currentReadModelSchema, childrenAccessorProperty, identifiedByProperty) = target;
        var rootReadModel = context.RootReadModel;
        var schema = rootReadModel.GetSchemaForLatestGeneration();
        var hasIdProperty = schema.HasKeyProperty();
        var actualIdentifiedByProperty = identifiedByProperty.IsRoot && hasIdProperty ? new PropertyPath(schema.GetKeyProperty().Name) : identifiedByProperty;

        // Create child projection structures first (without resolving events)
        var childProjectionTasks = projectionDefinition.Children.Select(async kvp =>
        {
            var childrenProperty = GetChildCollectionProperty(projectionId, rootReadModel, currentReadModelSchema, kvp.Key);
            return await CreateProjectionStructure(
                context,
                projectionId,
                new(kvp.Value, childrenProperty.Item?.ActualSchema ?? currentReadModelSchema, childrenAccessorProperty.AddArrayIndex(kvp.Key), kvp.Value.IdentifiedBy),
                $"{path} -> ChildrenAt({kvp.Key.Path})",
                generationDelivery,
                releaseStoredEvent);
        });

        var childResults = await Task.WhenAll(childProjectionTasks.ToArray());
        var childProjections = childResults.Select(r => r.Projection).Cast<IProjection>().ToArray();
        var initialState = GetInitialState(expandoObjectConverter, projectionDefinition, currentReadModelSchema);

        var noAutoMapProperties = (projectionDefinition.NoAutoMapProperties ?? [])
            .Select(_ => _.LastSegment.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var projection = new Projection(
            projectionDefinition.EventSequenceId,
            projectionId,
            initialState,
            path,
            childrenAccessorProperty,
            actualIdentifiedByProperty,
            rootReadModel,
            currentReadModelSchema,
            projectionDefinition.IsRewindable,
            projectionDefinition.AutoMap,
            noAutoMapProperties,
            childProjections,
            projectionDefinition.SubscribesToAllEvents,
            projectionDefinition.SubscribesToAllEvents ? keyResolvers.FromEventSourceId : null,
            projectionDefinition.Scope)
        {
            GenerationDelivery = generationDelivery,
            ReleaseStoredEvent = releaseStoredEvent
        };

        // Set parent relationships immediately after creation
        // This ensures children have their Parent set before any event resolution
        SetParentOnAllChildProjections(projection, childProjections);

        return (projection, childProjections, actualIdentifiedByProperty);
    }

    void ResolveEventsRecursively(Projection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool isChild)
    {
        // First resolve events for all children (depth-first)
        // At this point, all children already have their Parent set
        foreach (var childEntry in projectionDefinition.Children.Zip(childProjections, (kvp, child) => (kvp, child)))
        {
            if (childEntry.child is Projection childProjection)
            {
                var childDefinition = childEntry.kvp.Value;
                var childIdentifiedBy = childDefinition.IdentifiedBy.IsRoot ?
                    childProjection.IdentifiedByProperty :
                    childDefinition.IdentifiedBy;

                ResolveEventsRecursively(
                    childProjection,
                    childProjection.ChildProjections.ToArray(),
                    childDefinition,
                    childIdentifiedBy,
                    true);
            }
        }

        // Then resolve events for the current projection
        ResolveEventsForProjection(projection, childProjections, projectionDefinition, actualIdentifiedByProperty, isChild);
    }

    void SetupFromEventPropertyAndJoins(ProjectionLevel level, SubscriptionContext context)
    {
        var projection = level.Projection;
        var projectionDefinition = level.Definition;

        SetupFromEventProperty(level, context);

        // Resolved eagerly, not lazily, so that a dynamic property path is validated (and any
        // DynamicPropertyPathOnNonDynamicProperty raised) at projection-creation time, regardless of whether the
        // projection has any per-event-type `from` block to piggyback the enumeration on - an `all`-only
        // projection has none.
        var propertyMappersForEveryEventType = projectionDefinition.FromEvery.Properties.Select(kvp => ResolveEveryEventPropertyMapper(projection, level.AccessorPath + kvp.Key, kvp.Value)).ToList();
        var everyEventMappers = new EveryEventMappers(propertyMappersForEveryEventType);
        foreach (var (eventType, fromDefinition) in projectionDefinition.From)
        {
            SetupFromWithJoins(level, context, new(fromDefinition, eventType), everyEventMappers.ClaimFor(eventType.Id));
        }

        SetupAllEventsSubscription(level, propertyMappersForEveryEventType);

        SetupRemovedWith(level);
        SetupRemovedWithJoin(level);
        SetupNestedSubscriptions(level, context);

        foreach (var derivative in projectionDefinition.FromDerivatives ?? [])
        {
            foreach (var eventType in derivative.EventTypes)
            {
                SetupFromWithJoins(level, context, new(derivative.From, eventType), everyEventMappers.ClaimFor(eventType.Id));
            }
        }

        SetupJoinSubscriptions(
            level,
            context,
            everyEventMappers,
            projectionDefinition.From.Keys.Select(_ => _.Id)
                .Concat(projectionDefinition.FromDerivatives?.SelectMany(_ => _.EventTypes.Select(type => type.Id)) ?? [])
                .ToHashSet());

        ChildEveryEventMappings.Subscribe(projection, projectionDefinition.FromEvery, propertyMappersForEveryEventType);

        LogUnmappedCollectionProperties(level, context);
    }

    void SetupFromEventProperty(ProjectionLevel level, SubscriptionContext context)
    {
        var projection = level.Projection;
        var fromEventProperty = level.Definition.FromEventProperty;
        if (fromEventProperty is null)
        {
            return;
        }

        var schemaProperty = context.RootReadModel.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(level.AccessorPath);
        schemaProperty ??= new JsonSchemaProperty
        {
            Type = projection.TargetReadModelSchema.Type,
            Format = projection.TargetReadModelSchema.Format
        };

        var valueProvider = eventValueProviderExpressionResolvers.Resolve(schemaProperty, fromEventProperty.PropertyExpression);
        projection.Subscriptions.Add(
            projection.Event
                .WhereEventTypeEquals(fromEventProperty.Event, projection.GenerationDelivery)
                .AddChildFromEventProperty(level.AccessorPath, valueProvider).Subscribe());
    }

    void SetupFromWithJoins(ProjectionLevel level, SubscriptionContext context, FromRegistration registration, IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappersForAllEventTypes)
    {
        var fromObservable = SetupFromDefinition(level, context, registration, propertyMappersForAllEventTypes);
        SetupJoinsForFromDefinition(level, context, new(registration, fromObservable));
    }

    /// <summary>
    /// A projection that subscribes to all events (the `all` block / .FromAll()) has no per-event-type `From`
    /// registration to hang its every-event mappers off - by design, since the entire point is to also cover
    /// event types that do not exist yet. Give it one subscription against every event instead, skipping event
    /// types already mapped by root `from`, derivative or join registrations so those do not get the
    /// every-event mappers applied again. Removal registrations do not carry every-event mappers.
    /// Known limitation: a join-only event whose join matches no read model receives no all-event
    /// fallback under its own event source id. A fallback would require observing join matches
    /// before routing the event, rather than independently subscribing to the same event twice.
    /// </summary>
    /// <param name="level">The <see cref="ProjectionLevel"/> being set up.</param>
    /// <param name="propertyMappersForEveryEventType">The mappers applied to every event.</param>
    void SetupAllEventsSubscription(ProjectionLevel level, IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappersForEveryEventType)
    {
        var projection = level.Projection;
        var projectionDefinition = level.Definition;
        if (!projectionDefinition.SubscribesToAllEvents || level.IsChild)
        {
            return;
        }

        var mappedEventTypeIds = projectionDefinition.From.Keys
            .Concat(projectionDefinition.Join.Keys)
            .Concat(projectionDefinition.FromDerivatives?.SelectMany(_ => _.EventTypes) ?? [])
            .Concat(ChildEveryEventMappings.GetIncludedChildEventTypes(projection, projectionDefinition.FromEvery))
            .Select(_ => _.Id)
            .ToHashSet();
        projection.Event
            .Where(_ => !mappedEventTypeIds.Contains(_.Event.Context.EventType.Id))
            .Project(
                level.AccessorPath,
                level.IdentifiedBy,
                propertyMappersForEveryEventType,
                level.AccessorPath.IsRoot ? null : projection.InitialModelState,
                subscriptions: projection.Subscriptions);
    }

    void LogUnmappedCollectionProperties(ProjectionLevel level, SubscriptionContext context)
    {
        var projection = level.Projection;
        foreach (var unmapped in FindUnmappedCollectionProperties(level.Definition, level.Schema, projection.AutoMap, projection.NoAutoMapProperties, context.EventTypeSchemas))
        {
            var propertyPath = level.AccessorPath.IsRoot ? unmapped.Property : $"{level.AccessorPath.Path}.{unmapped.Property}";
            logger.CollectionPropertyAutoMapsToNothing(context.RootReadModel.DisplayName.ToString(), propertyPath, unmapped.EventTypes);
        }
    }

    void SetupJoinSubscriptions(
        ProjectionLevel level,
        SubscriptionContext context,
        EveryEventMappers everyEventMappers,
        HashSet<EventTypeId> keyedFromEventTypeIds)
    {
        var projection = level.Projection;
        var autoMap = level.AutoMapSettings;
        foreach (var (eventType, joinDefinition) in level.Definition.Join)
        {
            var mergedJoinProperties = GetMergedJoinProperties(joinDefinition, level.Schema, context.EventTypeSchemas.SchemaFor(eventType, projection.GenerationDelivery)?.Schema, autoMap.AutoMap, autoMap.NoAutoMapProperties);
            var propertyMappers = mergedJoinProperties.ConvertAll(kvp => ResolvePropertyMapper(projection, level.AccessorPath + kvp.Key, kvp.Value));
            propertyMappers.AddRange(everyEventMappers.ClaimFor(eventType.Id));
            var joinObservable = projection.Event
                .WhereEventTypeEquals(eventType, projection.GenerationDelivery)
                .Join(level.AccessorPath + joinDefinition.On, !level.IsNested && keyedFromEventTypeIds.Contains(eventType.Id));

            ProjectJoined(level, joinObservable, propertyMappers);
        }
    }

    IObservable<ProjectionEventContext> SetupFromDefinition(
        ProjectionLevel level,
        SubscriptionContext context,
        FromRegistration registration,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappersForAllEventTypes)
    {
        var projection = level.Projection;
        var schemaList = context.EventTypeSchemas.ToList();
        var matchingSchema = schemaList.SchemaFor(registration.EventType, projection.GenerationDelivery);
        var mergedFromProperties = GetMergedFromProperties(registration.Definition, level.Schema, matchingSchema?.Schema, projection.AutoMap, projection.NoAutoMapProperties);
        var propertyMappers = mergedFromProperties.ConvertAll(kvp => ResolvePropertyMapper(projection, level.AccessorPath + kvp.Key, kvp.Value));
        propertyMappers.AddRange(propertyMappersForAllEventTypes);
        return projection.Event
            .WhereEventTypeEquals(registration.EventType, projection.GenerationDelivery)
            .Project(
                level.AccessorPath,
                level.IdentifiedBy,
                propertyMappers,
                level.AccessorPath.IsRoot ? null : projection.InitialModelState,
                subscriptions: projection.Subscriptions);
    }

    void SetupJoinsForFromDefinition(ProjectionLevel level, SubscriptionContext context, FromSubscription from)
    {
        // Notes: The purpose of this method is to hook up on every From definition that matches the eventType of the Join definition
        // and the join definition matching the property its joining on to then add actions for resolving a join post a projection of
        // the "from".
        foreach (var join in FindJoinExpressions(level, context, from.Registration))
        {
            SetupResolvedJoin(level, context, from.Observable, join);
        }
    }

    void SetupResolvedJoin(ProjectionLevel level, SubscriptionContext context, IObservable<ProjectionEventContext> fromObservable, KeyValuePair<EventType, JoinDefinition> join)
    {
        var (joinEventType, joinDefinition) = join;
        var autoMap = level.AutoMapSettings;
        var joinEventSchema = context.EventTypeSchemas.SchemaFor(joinEventType, level.Projection.GenerationDelivery)?.Schema;
        var mergedJoinProperties = GetMergedJoinProperties(joinDefinition, level.Schema, joinEventSchema, autoMap.AutoMap, autoMap.NoAutoMapProperties);
        var joinPropertyMappers = mergedJoinProperties.Select(kvp => ResolvePropertyMapper(level.Projection, level.AccessorPath + kvp.Key, kvp.Value)).ToArray();
        var resolvedJoin = fromObservable.ResolveJoin(context.EventSequenceStorage, joinEventType, level.AccessorPath + joinDefinition.On, logger, eventCompliance, joinEventSchema, context.EventTypesStorage, level.Projection.GenerationDelivery == EventGenerationDelivery.Pinned ? storedEvent => level.Projection.ReleaseEvent(joinEventType, storedEvent) : null);
        ProjectJoined(level, resolvedJoin, joinPropertyMappers);
    }

    PropertyMapper<AppendedEvent, ExpandoObject> ResolveEveryEventPropertyMapper(Projection projection, PropertyPath propertyPath, string expression) =>
        EveryEventPropertyMappers.WhenSourceIsPresent(propertyPath, expression, ResolvePropertyMapper(projection, propertyPath, expression));

    PropertyMapper<AppendedEvent, ExpandoObject> ResolvePropertyMapper(Projection projection, PropertyPath propertyPath, string expression)
    {
        // Check if this is a dynamic property path (dictionary with runtime-determined keys)
        // Pattern: propertyName.$eventContext... or propertyName.$causedBy...
        var propertyPathString = propertyPath.Path;
        var isDynamicPropertyPath = propertyPathString.Contains(".$", StringComparison.Ordinal);

        // The path's own last segment (e.g. "id" in "eventCountByType.$eventContext.eventType.id") is part of
        // the dynamic key expression, not a real property on the read model - looking it up by that name would
        // risk matching an unrelated, identically-named top-level property and inheriting its schema (and
        // therefore its value type). A dictionary has no declared value schema of its own, so default to the
        // int64 shape count/increment/decrement/add/subtract need; a concrete base property is still honored
        // and validated below.
        var schemaProperty = isDynamicPropertyPath
            ? new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int64" }
            : ResolveStaticSchemaProperty(projection, propertyPath);

        if (isDynamicPropertyPath)
        {
            MarkBasePropertyAsDynamic(projection, propertyPath, propertyPathString);
        }

        return propertyMapperExpressionResolvers.Resolve(propertyPath, schemaProperty, expression);
    }

    void ResolveEventsForProjection(Projection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool hasParent)
    {
        logger.ResolveEventsForProjectionStart(projection.Path, projectionDefinition.From.Count, childProjections.Length);

        // Sets up the key resolver used for root resolution - meaning what identifies the object / document we're working on / projecting to.
        var fromEventTypes = projectionDefinition.From.Select(kvp => GetEventTypeWithKeyResolver(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, hasParent, kvp.Value.ParentKey)).ToArray();
        var joinEventTypes = projectionDefinition.Join.Select(kvp => GetEventTypeWithKeyResolverForJoin(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, kvp.Value.On)).ToArray();
        var removedWithEventTypes = projectionDefinition.RemovedWith.Select(kvp => GetEventTypeWithKeyResolver(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, hasParent, kvp.Value.ParentKey)).ToArray();
        var removedWithJoinEventTypes = hasParent
            ? projectionDefinition.RemovedWithJoin.Select(kvp => GetEventTypeWithKeyResolverForJoin(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, actualIdentifiedByProperty)).ToArray()
            : [];

        // Collect event types from all nested definitions (nested objects use the same key resolver as the parent)
        var nestedEventTypes = CollectNestedEventTypes(projection, projectionDefinition.Nested, actualIdentifiedByProperty, hasParent, projection.ChildrenPropertyPath);

        // Combine all operation-type contributions per event type. The same event type can be registered
        // through multiple mechanisms simultaneously — e.g. class-level [FromEvent<T>] on the parent +
        // a nested definition that also projects T via [SetFrom<T>] on its properties. We OR the
        // ProjectionOperationType flags together so a single event type can carry multiple operation
        // semantics (From + Remove, From + Join, etc.) without colliding on dictionary insert.
        // Derivative and from-event-property registrations count too: a parent derives ChildrenAffected from its
        // children's entries, so an event type a child handles only that way must be in the child's map or the
        // parent would treat the event as one that creates its own instance.
        var operationTypes = fromEventTypes.Select(_ => (_.EventType, Op: ProjectionOperationType.From))
            .Concat(joinEventTypes.Select(_ => (_.EventType, Op: ProjectionOperationType.Join)))
            .Concat(removedWithEventTypes.Select(_ => (_.EventType, Op: ProjectionOperationType.Remove)))
            .Concat(removedWithJoinEventTypes.Select(_ => (_.EventType, Op: ProjectionOperationType.Join | ProjectionOperationType.Remove)))
            .Concat(nestedEventTypes.Select(_ => (_.Event.EventType, Op: _.Operation)))
            .Concat(GetDerivativeAndEventPropertyOperations(projectionDefinition))
            .GroupBy(t => t.EventType)
            .ToDictionary(g => g.Key, g => g.Aggregate(ProjectionOperationType.None, (acc, x) => acc | x.Op));

        List<EventTypeWithKeyResolver> eventsForProjection = [.. fromEventTypes, .. joinEventTypes, .. removedWithEventTypes, .. removedWithJoinEventTypes, .. nestedEventTypes.Select(_ => _.Event)];
        foreach (var fromDerivativeDefinition in projectionDefinition.FromDerivatives ?? [])
        {
            eventsForProjection.AddRange(fromDerivativeDefinition.EventTypes.Select(eventType => GetEventTypeWithKeyResolver(projection, eventType, fromDerivativeDefinition.From.Key, actualIdentifiedByProperty, hasParent, fromDerivativeDefinition.From.ParentKey)));
        }

        if (projectionDefinition.FromEventProperty is not null)
        {
            eventsForProjection.Add(new EventTypeWithKeyResolver(projectionDefinition.FromEventProperty.Event, keyResolvers.FromEventSourceId, ResolvesToEventSourceId: true));
        }

        var distinctOwnEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).Select(_ => _.EventType).ToArray();

        foreach (var child in childProjections)
        {
            var childTypes = child.EventTypesWithKeyResolver.Where(_ => !eventsForProjection.Exists(e => e.EventType == _.EventType));
            logger.CollectingEventsFromChild(childTypes.Count(), child.Path);
            eventsForProjection.AddRange(childTypes);
        }

        // TODO: This has an implication in that only one key resolver can exist for each event type, meaning that an event type
        // can only be used once for a projection, including child projections.
        var distinctEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).ToArray();
        logger.ResolveEventsForProjectionComplete(distinctEventTypes.Length, projection.Path);

        // An event reached only by subscribing to every event type has no FromDefinition to carry a key, so the
        // one declared for the from-every clause is resolved here - where the projection it belongs to exists.
        // Defaulting to the event source id keeps every definition written before a key could be declared keying
        // exactly as it did.
        if (projectionDefinition.SubscribesToAllEvents)
        {
            SetAllEventsKeyResolver(projection, projectionDefinition, actualIdentifiedByProperty);
        }

        projection.SetEventTypesWithKeyResolvers(
            distinctEventTypes,
            distinctOwnEventTypes,
            operationTypes);
    }

    void SetAllEventsKeyResolver(Projection projection, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty)
    {
        var allEventsKey = projectionDefinition.FromEvery.Key;

        // The default key is the event source id expression, which has a resolver of its own and so comes back
        // from GetKeyResolverFor reporting that it did not resolve to the event source id. It plainly does, and
        // saying otherwise costs every existing from-every projection its fine-grained locking.
        var resolvesToEventSourceId =
            allEventsKey is null ||
            allEventsKey.Value.Length == 0 ||
            allEventsKey.Value == WellKnownExpressions.EventSourceId;

        projection.SetAllEventsKeyResolver(
            GetKeyResolverFor(projection, allEventsKey, actualIdentifiedByProperty).Resolver,
            resolvesToEventSourceId);
    }

    List<(EventTypeWithKeyResolver Event, ProjectionOperationType Operation)> CollectNestedEventTypes(
        Projection projection,
        IDictionary<PropertyPath, ChildrenDefinition>? nested,
        PropertyPath actualIdentifiedByProperty,
        bool hasParent,
        PropertyPath parentPath)
    {
        if (nested is null || nested.Count == 0)
        {
            return [];
        }

        var result = new List<(EventTypeWithKeyResolver Event, ProjectionOperationType Operation)>();

        foreach (var kvp in nested)
        {
            var nestedDefinition = kvp.Value;
            var nestedPath = parentPath.IsRoot ? kvp.Key : parentPath + kvp.Key;

            // Nested objects live in the parent document, but joins still resolve against their full property path.
            result.AddRange(nestedDefinition.From.Select(f => (
                GetEventTypeWithKeyResolver(projection, f.Key, f.Value.Key, actualIdentifiedByProperty, hasParent, f.Value.ParentKey),
                ProjectionOperationType.From)));

            if (!hasParent)
            {
                result.AddRange(nestedDefinition.Join.Select(j => (
                    GetEventTypeWithKeyResolverForJoin(projection, j.Key, j.Value.Key, actualIdentifiedByProperty, nestedPath + j.Value.On),
                    ProjectionOperationType.Join)));
            }

            result.AddRange(nestedDefinition.RemovedWith.Select(r => (
                GetEventTypeWithKeyResolver(projection, r.Key, r.Value.Key, actualIdentifiedByProperty, hasParent, r.Value.ParentKey),
                ProjectionOperationType.From)));

            result.AddRange(CollectNestedEventTypes(projection, nestedDefinition.Nested, actualIdentifiedByProperty, hasParent, nestedPath));
        }

        return result;
    }

    EventTypeWithKeyResolver GetEventTypeWithKeyResolverForJoin(Projection projection, EventType eventType, PropertyExpression key, PropertyPath actualIdentifiedByProperty, PropertyPath joinOnProperty)
    {
        // A ROOT join resolves its declared event property (or defaults to the event source id) without
        // coercion to the read model's key type: the join-ON value can be a string while the root key is a Guid.
        // ForJoin matches it against existing roots, leaving row-creation-time ResolveJoin backfill unchanged.
        // A CHILD join keeps the key-expression resolver: its resolved value becomes the child's array-indexer
        // identifier, which must be typed to the child's identified-by property to locate the child.
        var keyResolver = projection.HasParent
            ? GetKeyResolverFor(projection, key, actualIdentifiedByProperty).Resolver
            : GetRootJoinKeyResolver(key);
        keyResolver = keyResolvers.ForJoin(projection, keyResolver, actualIdentifiedByProperty, joinOnProperty);

        // A join matches the event against existing roots by a joined value, collapsing distinct event sources onto
        // one document, so it is never purely event-source-keyed.
        return new EventTypeWithKeyResolver(eventType, keyResolver);
    }

    KeyResolver GetRootJoinKeyResolver(PropertyExpression key)
    {
        if (!CanResolveKey(key) || !new EventContentExpressionResolver().CanResolve(key))
        {
            return keyResolvers.FromEventSourceId;
        }

        // A missing property falls back to the event source id, as a root join did before it honored its key.
        return keyResolvers.FromEventValueProviderWithFallbackToEventSourceId(new EventContentExpressionResolver().Resolve(key));
    }

    void WarnUnsupportedRootJoinKey(Projection projection, ProjectionDefinition definition)
    {
        if (FindUnsupportedRootJoinKey(definition) is { } key)
        {
            logger.UnsupportedRootJoinKey(projection.Identifier.Value, key.Value);
        }

        static PropertyExpression? FindUnsupportedRootJoinKey(ProjectionDefinition definition)
        {
            var unsupportedKey = definition.Join.Values.Select(join => join.Key).FirstOrDefault(key =>
                !string.IsNullOrEmpty(key.Value) && key != WellKnownExpressions.EventSourceId &&
                !new EventContentExpressionResolver().CanResolve(key));
            return unsupportedKey ?? definition.Nested?.Values.Select(FindUnsupportedRootJoinKey).FirstOrDefault(key => key is not null);
        }
    }

    EventTypeWithKeyResolver GetEventTypeWithKeyResolver(Projection projection, EventType eventType, PropertyExpression key, PropertyPath actualIdentifiedByProperty, bool hasParent, PropertyExpression? parentKey)
    {
        logger.GetEventTypeWithKeyResolverStart(
            eventType.Id.ToString(),
            hasParent,
            parentKey?.Value,
            projection.HasParent,
            projection.HasParent ? projection.Parent!.IdentifiedByProperty.Path : "N/A");

        var (keyResolver, resolvesToEventSourceId) = GetKeyResolverFor(projection, key, actualIdentifiedByProperty);
        if (!hasParent)
        {
            return new EventTypeWithKeyResolver(eventType, keyResolver, resolvesToEventSourceId);
        }

        // Default to EventSourceId for parent key when not explicitly specified
        // This supports the parent-keyed pattern where the EventSourceId represents the parent's identifier
        var effectiveParentKey = parentKey;
        if (effectiveParentKey is null || string.IsNullOrEmpty(effectiveParentKey.Value))
        {
            effectiveParentKey = new PropertyExpression(WellKnownExpressions.EventSourceId);
            logger.GetEventTypeWithKeyResolverInferredParentKey(effectiveParentKey.Value, true);
        }

        var parentProjection = projection.HasParent ? projection.Parent! : projection;
        var parentIdentifiedByProperty = projection.HasParent ? projection.Parent!.IdentifiedByProperty : actualIdentifiedByProperty;
        var parentKeyResolver = GetParentKeyResolverFor(parentProjection, effectiveParentKey, parentIdentifiedByProperty);
        keyResolver = keyResolvers.FromParentHierarchy(projection, keyResolver, parentKeyResolver, actualIdentifiedByProperty, IsConstantExpression(effectiveParentKey));

        // A parent-hierarchy resolver routes the child event to its parent document, collapsing distinct event
        // sources onto one document, so it is never purely event-source-keyed.
        return new EventTypeWithKeyResolver(eventType, keyResolver);
    }

    (KeyResolver Resolver, bool ResolvesToEventSourceId) GetKeyResolverFor(IProjection projection, PropertyExpression? key, PropertyPath actualIdentifiedByProperty)
    {
        if (CanResolveKey(key))
        {
            return (keyExpressionResolvers.Resolve(projection, key, actualIdentifiedByProperty),
                key.Value == WellKnownExpressions.EventSourceId && KeyConversionPreservesIdentity(projection, actualIdentifiedByProperty));
        }

        return (keyResolvers.FromEventSourceId, true);
    }

    KeyResolver GetParentKeyResolverFor(IProjection projection, PropertyExpression? key, PropertyPath actualIdentifiedByProperty)
    {
        logger.GetParentKeyResolverFor(key?.Value, false);

        if (CanResolveKey(key))
        {
            return keyExpressionResolvers.Resolve(projection, key, actualIdentifiedByProperty);
        }

        return keyResolvers.FromEventSourceId;
    }

    bool CanResolveKey([NotNullWhen(true)] PropertyExpression? key) =>
        key is not null && key.Value.Length != 0 && keyExpressionResolvers.CanResolve(key);

    sealed record ProjectionTarget(ProjectionDefinition Definition, JsonSchema Schema, PropertyPath AccessorPath, PropertyPath IdentifiedBy);

    sealed class FingerprintEventDefinitionsConverter<TDefinition> : DictionaryJsonConverter<EventType, TDefinition>
    {
        protected override EventType GetKeyFromString(string key) => EventType.Parse(key);
        protected override string GetKeyString(EventType key) => key.ToString();
    }

    sealed record SubscriptionContext(
        IEventSequenceStorage EventSequenceStorage,
        IEventTypesStorage? EventTypesStorage,
        IEnumerable<EventTypeSchema> EventTypeSchemas,
        ReadModelDefinition RootReadModel);

    sealed record AutoMapSettings(AutoMap AutoMap, IReadOnlySet<string> NoAutoMapProperties);

    sealed record ProjectionLevel(
        Projection Projection,
        ProjectionTarget Target,
        bool IsChild,
        bool IsNested = false,
        AutoMapSettings? NestedAutoMap = null)
    {
        public ProjectionDefinition Definition => Target.Definition;

        public JsonSchema Schema => Target.Schema;

        public PropertyPath AccessorPath => Target.AccessorPath;

        public PropertyPath IdentifiedBy => Target.IdentifiedBy;

        public AutoMapSettings AutoMapSettings => NestedAutoMap ?? new(Projection.AutoMap, Projection.NoAutoMapProperties);
    }

    sealed record FromRegistration(FromDefinition Definition, EventType EventType);

    sealed record FromSubscription(FromRegistration Registration, IObservable<ProjectionEventContext> Observable);

    sealed class EveryEventMappers(List<PropertyMapper<AppendedEvent, ExpandoObject>> mappers)
    {
        readonly HashSet<EventTypeId> _mappedEventTypeIds = [];

        public List<PropertyMapper<AppendedEvent, ExpandoObject>> ClaimFor(EventTypeId eventTypeId) =>
            _mappedEventTypeIds.Add(eventTypeId) ? mappers : [];
    }
}
