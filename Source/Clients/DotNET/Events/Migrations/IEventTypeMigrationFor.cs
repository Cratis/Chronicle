// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a migrator for a specific event type.
/// </summary>
/// <typeparam name="TEvent">The event type to migrate.</typeparam>
public interface IEventTypeMigrationFor<TEvent> : IEventTypeMigration;

/// <summary>
/// Defines a type-safe migrator between two generations of the same event type.
/// </summary>
/// <typeparam name="TUpgrade">The upgraded (newer generation) event type.</typeparam>
/// <typeparam name="TPrevious">The previous (older generation) event type.</typeparam>
/// <remarks>
/// Implementors should extend <see cref="EventTypeMigration{TUpgrade, TPrevious}"/> instead of
/// implementing this interface directly. The abstract base class handles generation extraction,
/// validation, and the untyped <see cref="IEventTypeMigration"/> plumbing automatically.
/// </remarks>
public interface IEventTypeMigrationFor<TUpgrade, TPrevious> : IEventTypeMigrationFor<TUpgrade>
{
    /// <summary>
    /// Define the type-safe upcast migration from <typeparamref name="TPrevious"/> to <typeparamref name="TUpgrade"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationBuilder{TUpgrade, TPrevious}"/> to use.</param>
    void Upcast(IEventMigrationBuilder<TUpgrade, TPrevious> builder);

    /// <summary>
    /// Define the type-safe downcast migration from <typeparamref name="TUpgrade"/> to <typeparamref name="TPrevious"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationBuilder{TPrevious, TUpgrade}"/> to use.</param>
    void Downcast(IEventMigrationBuilder<TPrevious, TUpgrade> builder);
}
