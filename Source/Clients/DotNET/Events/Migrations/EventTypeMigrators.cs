// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Cratis.Reflection;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventTypeMigrators"/>.
/// </summary>
/// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for discovering migrators.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> for creating instances.</param>
public class EventTypeMigrators(IClientArtifactsProvider clientArtifactsProvider, IServiceProvider serviceProvider) : IEventTypeMigrators
{
    readonly Dictionary<Type, List<IEventTypeMigration>> _migratorsByEventType = [];
    readonly Dictionary<Type, IEventTypeMigration> _explicitInstances = [];
    readonly Dictionary<Type, ExplicitMigrationMethods> _plainMethods = [];
    IEventTypes? _eventTypes;
    HashSet<Type> _explicitTypes = [];

    /// <inheritdoc/>
    public IEnumerable<Type> AllMigrators => clientArtifactsProvider.EventTypeMigrators;

    /// <inheritdoc/>
    /// <exception cref="MultipleMigratorsForSameEventTypeGeneration">
    /// Thrown when more than one discovered migrator bridges the same pair of generations for <paramref name="eventType"/>.
    /// </exception>
    public IEnumerable<IEventTypeMigration> GetMigratorsFor(Type eventType)
    {
        if (!_migratorsByEventType.TryGetValue(eventType, out var migrators))
        {
            migrators = [];
            var migratorTypes = clientArtifactsProvider.EventTypeMigrators
                .Where(t => t.HasInterface(typeof(IEventTypeMigrationFor<>).MakeGenericType(eventType)) ||
                    (_eventTypes is not null && _explicitTypes.Contains(t) && !t.IsAssignableTo(typeof(IEventTypeMigration)) && PlainMethodsFor(t).UpgradeType == eventType));

            var originalTypes = new Dictionary<IEventTypeMigration, Type>(ReferenceEqualityComparer.Instance);
            foreach (var migratorType in migratorTypes)
            {
                var migrator = _eventTypes is null
                    ? (IEventTypeMigration)ActivatorUtilities.CreateInstance(serviceProvider, migratorType)
                    : Activate(migratorType);
                migrators.Add(migrator);
                originalTypes[migrator] = migratorType;
            }

            var duplicate = migrators
                .GroupBy(_ => (_.From, _.To))
                .FirstOrDefault(_ => _.Count() > 1);
            if (duplicate is not null)
            {
                var identity = _eventTypes is not null && duplicate.Any(_ => _ is IRegistryBoundEventTypeMigration { Registry: not null })
                    ? _eventTypes.GetEventTypeFor(eventType)
                    : eventType.GetEventType();
                throw new MultipleMigratorsForSameEventTypeGeneration(
                    identity,
                    duplicate.Key.From,
                    duplicate.Key.To,
                    duplicate.Select(_ => originalTypes[_]));
            }

            _migratorsByEventType[eventType] = migrators;
        }

        return migrators;
    }

    /// <summary>
    /// Gets whether the recipe requests owning-registry constructor injection.
    /// </summary>
    /// <param name="type">The original recipe type.</param>
    /// <returns>Whether a registry is required for activation, independently of the base validation path.</returns>
    internal static bool RequiresRegistry(Type type) => !type.IsAssignableTo(typeof(IEventTypeMigration)) ||
        type.GetConstructors().Any(_ => _.GetParameters().Any(p => p.ParameterType == typeof(IEventTypes)));

    /// <summary>
    /// Creates an independent migration provider bound to one finalized store registry.
    /// </summary>
    /// <param name="artifacts">The immutable store discovery revision.</param>
    /// <param name="eventTypes">The finalized owning event registry.</param>
    /// <returns>A new provider and cache; the client-shared provider is never rebound.</returns>
    internal EventTypeMigrators ForStore(IClientArtifactsProvider artifacts, IEventTypes eventTypes) => new(artifacts, serviceProvider)
    {
        _eventTypes = eventTypes,
        _explicitTypes = artifacts is ExplicitArtifactCatalog catalog ? catalog.ExplicitMigrations.ToHashSet() : new HashSet<Type>()
    };

    /// <summary>
    /// Validates every explicit recipe and builds both directions, including unreachable recipes.
    /// </summary>
    /// <param name="namingPolicy">The payload naming policy.</param>
    /// <exception cref="InvalidExplicitArtifact">An explicit migration has no registered upgrade destination.</exception>
    internal void ValidateExplicitMigrations(INamingPolicy namingPolicy)
    {
        foreach (var type in _explicitTypes)
        {
            var upgrades = UpgradeTypesFor(type).ToArray();
            if (!upgrades.Any(_ => _eventTypes!.HasFor(_)))
            {
                throw new InvalidExplicitArtifact(type, "migration", "No registered upgrade destination can select this recipe.");
            }

            var migration = Activate(type);
            migration.Upcast(new EventMigrationBuilder(namingPolicy));
            migration.Downcast(new EventMigrationBuilder(namingPolicy));
        }
    }

    /// <summary>
    /// Gets the recipe's declared upgrade association without inferring an artifact from its shape.
    /// </summary>
    /// <param name="type">The explicitly nominated recipe.</param>
    /// <returns>The supported upgrade associations.</returns>
    internal IEnumerable<Type> UpgradeTypesFor(Type type) => type.IsAssignableTo(typeof(IEventTypeMigration))
        ? type.GetInterfaces().Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IEventTypeMigrationFor<>)).Select(_ => _.GetGenericArguments()[0])
        : [PlainMethodsFor(type).UpgradeType];

    /// <summary>
    /// Gets the explicit instance already validated for this store revision.
    /// </summary>
    /// <param name="type">The nominated recipe type.</param>
    /// <returns>The store-local instance.</returns>
    internal IEventTypeMigration GetExplicitInstance(Type type) => _explicitInstances[type];

    IEventTypeMigration Activate(Type type)
    {
        if (_explicitInstances.TryGetValue(type, out var existing)) return existing;
        var migration = type.IsAssignableTo(typeof(IEventTypeMigration))
            ? (IEventTypeMigration)(RequiresRegistry(type)
                ? ActivatorUtilities.CreateInstance(serviceProvider, type, _eventTypes!)
                : ActivatorUtilities.CreateInstance(serviceProvider, type))
            : PlainMethodsFor(type).Create(serviceProvider, _eventTypes!);
        if (_explicitTypes.Contains(type)) _explicitInstances[type] = migration;

        return migration;
    }

    ExplicitMigrationMethods PlainMethodsFor(Type type)
    {
        if (_plainMethods.TryGetValue(type, out var existing)) return existing;
        var methods = ExplicitMigrationMethods.For(type);
        _plainMethods[type] = methods;

        return methods;
    }
}
