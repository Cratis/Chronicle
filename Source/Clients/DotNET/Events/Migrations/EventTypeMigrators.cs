// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Reflection;
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

    /// <inheritdoc/>
    public IEnumerable<Type> AllMigrators => clientArtifactsProvider.EventTypeMigrators;

    /// <inheritdoc/>
    public IEnumerable<IEventTypeMigration> GetMigratorsFor(Type eventType)
    {
        if (!_migratorsByEventType.TryGetValue(eventType, out var migrators))
        {
            migrators = [];
            var migratorTypes = clientArtifactsProvider.EventTypeMigrators
                .Where(t => t.HasInterface(typeof(IEventTypeMigrationFor<>).MakeGenericType(eventType)));

            foreach (var migratorType in migratorTypes)
            {
                var migrator = (IEventTypeMigration)ActivatorUtilities.CreateInstance(serviceProvider, migratorType);
                migrators.Add(migrator);
            }

            _migratorsByEventType[eventType] = migrators;
        }

        return migrators;
    }
}
