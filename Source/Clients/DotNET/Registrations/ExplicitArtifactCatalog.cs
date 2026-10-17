// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// An immutable discovery revision merged with the store's original options snapshot.
/// </summary>
internal sealed class ExplicitArtifactCatalog : IClientArtifactsProvider
{
    readonly EventTypeDeclarations _eventDeclarations;
    readonly FrozenDictionary<(Type Type, ExplicitArtifactKind Kind), string> _identities;
    readonly FrozenSet<(Type Type, ExplicitArtifactKind Kind)> _nominations;

    /// <summary>
    /// Initializes a new instance of <see cref="ExplicitArtifactCatalog"/>.
    /// </summary>
    /// <param name="discovered">The native discovery provider, copied rather than mutated.</param>
    /// <param name="snapshot">The store's immutable declarations.</param>
    internal ExplicitArtifactCatalog(IClientArtifactsProvider discovered, ExplicitArtifactsSnapshot snapshot)
    {
        foreach (var recipe in snapshot.EventTypes)
        {
            ValidateConcreteType(recipe.Type, "event type");
        }
        foreach (var recipe in snapshot.Types)
        {
            ValidateConcreteType(recipe.Type, recipe.Kind.ToString());
        }
        ValidateObserverKinds(snapshot.Types);
        _nominations = snapshot.Types.Select(_ => (_.Type, _.Kind)).ToFrozenSet();

        EventTypes = Union(discovered.EventTypes, snapshot.EventTypes.Select(_ => _.Type));
        Projections = Copy(discovered.Projections);
        ModelBoundProjections = Copy(discovered.ModelBoundProjections);
        Reactors = Union(discovered.Reactors, Nominated(ExplicitArtifactKind.Reactor));
        Reducers = Union(discovered.Reducers, Nominated(ExplicitArtifactKind.Reducer));
        ProjectionReadModelTypes = snapshot.Projections.Select(projection => projection.ReadModelType).ToImmutableArray();
        ConstraintTypes = Union(discovered.ConstraintTypes, Nominated(ExplicitArtifactKind.Constraint));
        EventSources = Union(discovered.EventSources, Nominated(ExplicitArtifactKind.EventSource));
        Triggers = Copy(discovered.Triggers);
        OccurrenceHandlers = Copy(discovered.OccurrenceHandlers);
        EventTypeMigrators = Union(discovered.EventTypeMigrators, Nominated(ExplicitArtifactKind.Migration));
        ReadModelReactors = Copy(discovered.ReadModelReactors);
        ReactorMiddlewares = Copy(discovered.ReactorMiddlewares);
        ComplianceForTypesProviders = Copy(discovered.ComplianceForTypesProviders);
        ComplianceForPropertiesProviders = Copy(discovered.ComplianceForPropertiesProviders);
        SecurityForTypesProviders = Copy(discovered.SecurityForTypesProviders);
        SecurityForPropertiesProviders = Copy(discovered.SecurityForPropertiesProviders);
        AdditionalEventInformationProviders = Copy(discovered.AdditionalEventInformationProviders);
        EventSeeders = Copy(discovered.EventSeeders);
        var nominatedEvents = snapshot.EventTypes.Select(_ => _.Type).Distinct().ToArray();
        UniqueConstraints = Union(discovered.UniqueConstraints, nominatedEvents.Where(_ => _.GetProperties().Any(p => p.IsDefined(typeof(UniqueAttribute)))));
        UniqueEventTypeConstraints = Union(discovered.UniqueEventTypeConstraints, nominatedEvents.Where(_ => _.IsDefined(typeof(UniqueAttribute))));
        RemoveConstraintEventTypes = Union(discovered.RemoveConstraintEventTypes, nominatedEvents.Where(_ => _.IsDefined(typeof(RemoveConstraintAttribute))));
        ClosesStreamEventTypes = Union(discovered.ClosesStreamEventTypes, nominatedEvents.Where(_ => _.IsDefined(typeof(ClosesStreamAttribute))));

        _eventDeclarations = new(EventTypes, snapshot.EventTypes);
        _identities = snapshot.Types.GroupBy(_ => (_.Type, _.Kind)).ToFrozenDictionary(_ => _.Key, MergeIdentity);
        ExplicitMigrations = Nominated(ExplicitArtifactKind.Migration).Distinct().ToImmutableArray();

        IEnumerable<Type> Nominated(ExplicitArtifactKind kind) => snapshot.Types.Where(_ => _.Kind == kind).Select(_ => _.Type);
    }

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Projections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelReactors { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForTypesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForPropertiesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ClosesStreamEventTypes { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> EventSources { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Triggers { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> OccurrenceHandlers { get; }

    /// <summary>
    /// Gets every explicitly nominated migration, including ones unreachable from discovery's upgrades.
    /// </summary>
    internal ImmutableArray<Type> ExplicitMigrations { get; }

    /// <summary>
    /// Gets the models claimed by captured explicit projection recipes.
    /// </summary>
    internal ImmutableArray<Type> ProjectionReadModelTypes { get; }

    /// <summary>
    /// Gets the current immutable catalog without changing manual discovery or capturing new options.
    /// </summary>
    /// <param name="provider">The store or direct artifact provider.</param>
    /// <returns>The current catalog, if explicit discovery has run.</returns>
    internal static ExplicitArtifactCatalog? From(IClientArtifactsProvider provider) => provider switch
    {
        ExplicitArtifactCatalog catalog => catalog,
        StoreClientArtifactsProvider store => store.Current as ExplicitArtifactCatalog,
        _ => null
    };

    /// <summary>
    /// Checks explicit nomination independently of marker discovery.
    /// </summary>
    /// <param name="type">The artifact CLR type.</param>
    /// <param name="kind">The nominated kind.</param>
    /// <returns>Whether the snapshot explicitly nominated this artifact.</returns>
    internal bool IsExplicit(Type type, ExplicitArtifactKind kind) => _nominations.Contains((type, kind));

    /// <summary>
    /// Gets effective store metadata without requiring event markers.
    /// </summary>
    /// <param name="type">The registered event type.</param>
    /// <param name="currentStore">The current store.</param>
    /// <returns>The merged event descriptor.</returns>
    internal EventTypeDescriptor GetEventDescriptorFor(Type type, EventStoreName currentStore) => _eventDeclarations.GetDescriptorFor(type, currentStore);

    /// <summary>
    /// Resolves the registered reactor's peer-merged identity.
    /// </summary>
    /// <param name="type">The reactor type.</param>
    /// <returns>The reactor identifier.</returns>
    internal ReactorId GetReactorIdFor(Type type) => GetIdentity(type, ExplicitArtifactKind.Reactor);

    /// <summary>
    /// Resolves the registered reducer's peer-merged identity.
    /// </summary>
    /// <param name="type">The reducer type.</param>
    /// <returns>The reducer identifier.</returns>
    internal ReducerId GetReducerIdFor(Type type) => GetIdentity(type, ExplicitArtifactKind.Reducer);

    /// <summary>
    /// Resolves the registered event source's peer-merged name.
    /// </summary>
    /// <param name="type">The source type.</param>
    /// <returns>The event source name.</returns>
    internal string GetEventSourceNameFor(Type type) => GetIdentity(type, ExplicitArtifactKind.EventSource);

    static ImmutableArray<Type> Copy(IEnumerable<Type> types) => types.Distinct().ToImmutableArray();

    static ImmutableArray<Type> Union(IEnumerable<Type> first, IEnumerable<Type> second) => Copy(first.Concat(second));

    static void ValidateConcreteType(Type type, string kind)
    {
        if (type.IsAbstract || type.ContainsGenericParameters || type.IsInterface || type.IsPointer || type.IsByRef || type == typeof(void))
        {
            throw new InvalidExplicitArtifact(type, kind, "A concrete closed CLR type is required.");
        }
    }

    static void ValidateObserverKinds(IEnumerable<ExplicitArtifactRegistration> declarations)
    {
        foreach (var group in declarations.GroupBy(_ => _.Type))
        {
            var isReactor = group.Any(_ => _.Kind == ExplicitArtifactKind.Reactor);
            var isReducer = group.Any(_ => _.Kind == ExplicitArtifactKind.Reducer);
            if (isReactor || isReducer) RegisteredObserverMetadata.ValidateKinds(group.Key, isReactor, isReducer);
        }
    }

    static string MergeIdentity(IGrouping<(Type Type, ExplicitArtifactKind Kind), ExplicitArtifactRegistration> declarations)
    {
        var (type, kind) = declarations.Key;
        var identity = MarkerIdentity(type, kind);
        foreach (var recipe in declarations)
        {
            identity = DeclaredFacet<string>.Merge(type, "identity", identity, recipe.Identity);
        }

        return identity?.Value ?? DefaultIdentity(type, kind);
    }

    static DeclaredFacet<string>? MarkerIdentity(Type type, ExplicitArtifactKind kind) => kind switch
    {
        ExplicitArtifactKind.Reactor when type.GetCustomAttribute<ReactorAttribute>() is { } marker =>
            new(ArtifactIdentity.ObserverIdFor(type, marker.Id.Value), nameof(ReactorAttribute)),
        ExplicitArtifactKind.Reducer when type.GetCustomAttribute<ReducerAttribute>() is { } marker =>
            new(ArtifactIdentity.ObserverIdFor(type, marker.Id.Value), nameof(ReducerAttribute)),
        ExplicitArtifactKind.EventSource when type.GetCustomAttribute<EventSourceAttribute>() is { Name: not null } marker =>
            new(marker.Name, nameof(EventSourceAttribute)),
        _ => null
    };

    static string DefaultIdentity(Type type, ExplicitArtifactKind kind) => kind switch
    {
        ExplicitArtifactKind.EventSource => ArtifactIdentity.EventSourceNameFor(type),
        ExplicitArtifactKind.Constraint => ArtifactIdentity.ConstraintNameFor(type, null),
        _ => ArtifactIdentity.ObserverIdFor(type, null)
    };

    string GetIdentity(Type type, ExplicitArtifactKind kind) =>
        _identities.TryGetValue((type, kind), out var identity) ? identity : MarkerIdentity(type, kind)?.Value ?? DefaultIdentity(type, kind);
}
