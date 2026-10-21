// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Preserves a custom provider's legacy results while overlaying explicitly nominated store-bound recipes.
/// </summary>
/// <param name="original">The unchanged custom provider.</param>
/// <param name="explicitProvider">The independent provider for explicit recipe activation.</param>
/// <param name="catalog">The owning preparation revision.</param>
/// <param name="eventTypes">The owning finalized registry.</param>
internal sealed class StoreEventTypeMigrators(IEventTypeMigrators original, EventTypeMigrators explicitProvider, ExplicitArtifactCatalog catalog, IEventTypes eventTypes) : IEventTypeMigrators
{
    /// <inheritdoc/>
    public IEnumerable<Type> AllMigrators => original.AllMigrators.Concat(catalog.ExplicitMigrations).Distinct();

    /// <inheritdoc/>
    public IEnumerable<IEventTypeMigration> GetMigratorsFor(Type eventType)
    {
        var applicable = catalog.ExplicitMigrations.Where(_ => explicitProvider.UpgradeTypesFor(_).Contains(eventType)).ToArray();
        var advertised = original.AllMigrators.ToHashSet();
        var replacements = applicable.Where(type => !advertised.Contains(type) ||
            explicitProvider.GetExplicitInstance(type) is IRegistryBoundEventTypeMigration { Registry: not null }).ToArray();
        if (replacements.Length == 0) return original.GetMigratorsFor(eventType);

        var existing = original.GetMigratorsFor(eventType).ToArray();
        foreach (var type in replacements.Where(advertised.Contains))
        {
            if (!existing.Any(_ => _.GetType() == type))
            {
                throw new ExplicitMigrationBindingNotSupported(original.GetType());
            }
        }

        var entries = existing.Where(_ => !replacements.Contains(_.GetType())).Select(_ => (Migration: _, Recipe: _.GetType()))
            .Concat(replacements.Select(type => (Migration: explicitProvider.GetExplicitInstance(type), Recipe: type))).ToArray();
        var duplicate = entries.GroupBy(_ => (_.Migration.From, _.Migration.To)).FirstOrDefault(_ => _.Count() > 1);
        if (duplicate is not null)
        {
            throw new MultipleMigratorsForSameEventTypeGeneration(
                eventTypes.GetEventTypeFor(eventType),
                duplicate.Key.From,
                duplicate.Key.To,
                duplicate.Select(_ => _.Recipe));
        }

        return entries.Select(_ => _.Migration).ToArray();
    }

    /// <summary>
    /// Constructs store binding without mutating the client-shared provider or demanding custom rebinding for legacy-only stores.
    /// </summary>
    /// <param name="original">The client-shared migration provider.</param>
    /// <param name="artifacts">The immutable discovery revision.</param>
    /// <param name="eventTypes">The owning finalized registry.</param>
    /// <param name="serviceProvider">The store's ordinary constructor dependency provider.</param>
    /// <param name="namingPolicy">The payload naming policy.</param>
    /// <returns>The store-local migration provider.</returns>
    internal static IEventTypeMigrators Bind(IEventTypeMigrators original, IClientArtifactsProvider artifacts, IEventTypes eventTypes, IServiceProvider? serviceProvider, INamingPolicy namingPolicy)
    {
        if (original is EventTypeMigrators native)
        {
            var bound = native.ForStore(artifacts, eventTypes);
            bound.ValidateExplicitMigrations(namingPolicy);
            return bound;
        }
        if (artifacts is not ExplicitArtifactCatalog { ExplicitMigrations.Length: > 0 } catalog) return original;
        var explicitProvider = new EventTypeMigrators(artifacts, serviceProvider ?? new NoServices()).ForStore(artifacts, eventTypes);
        explicitProvider.ValidateExplicitMigrations(namingPolicy);

        return new StoreEventTypeMigrators(original, explicitProvider, catalog, eventTypes);
    }

    sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
