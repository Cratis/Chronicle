// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a migrator for a specific event type.
/// </summary>
/// <typeparam name="TEvent">The event type to migrate.</typeparam>
public interface IEventTypeMigrationFor<TEvent>
{
    /// <summary>
    /// Gets the generation to migrate from.
    /// </summary>
    uint From { get; }

    /// <summary>
    /// Gets the generation to migrate to.
    /// </summary>
    uint To { get; }

    /// <summary>
    /// Define the upcast migration.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationBuilder"/> to use.</param>
    void Upcast(IEventMigrationBuilder builder);

    /// <summary>
    /// Define the downcast migration.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationBuilder"/> to use.</param>
    void Downcast(IEventMigrationBuilder builder);
}
