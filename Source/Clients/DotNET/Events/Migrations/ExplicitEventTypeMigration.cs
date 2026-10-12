// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Adapts an explicitly nominated plain recipe through the existing typed migration engine.
/// </summary>
/// <typeparam name="TMigration">The original plain recipe class.</typeparam>
/// <typeparam name="TUpgrade">The upgraded event type.</typeparam>
/// <typeparam name="TPrevious">The previous event type.</typeparam>
internal sealed class ExplicitEventTypeMigration<TMigration, TUpgrade, TPrevious> : EventTypeMigration<TUpgrade, TPrevious>
    where TMigration : class
{
    readonly Action<IEventMigrationBuilder<TUpgrade, TPrevious>> _upcast;
    readonly Action<IEventMigrationBuilder<TPrevious, TUpgrade>> _downcast;
    readonly Action<IEventValueMapBuilder<TUpgrade, TPrevious>>? _mapValues;

    /// <summary>
    /// Initializes a new instance of <see cref="ExplicitEventTypeMigration{TMigration, TUpgrade, TPrevious}"/>.
    /// </summary>
    /// <param name="eventTypes">The owning store's finalized registry.</param>
    /// <param name="migration">The plain recipe instance, activated once for this store revision.</param>
    /// <param name="methods">The validated typed roles of the original recipe.</param>
    internal ExplicitEventTypeMigration(IEventTypes eventTypes, TMigration migration, ExplicitMigrationMethods methods)
        : base(eventTypes)
    {
        _upcast = methods.Upcast.CreateDelegate<Action<IEventMigrationBuilder<TUpgrade, TPrevious>>>(migration);
        _downcast = methods.Downcast.CreateDelegate<Action<IEventMigrationBuilder<TPrevious, TUpgrade>>>(migration);
        _mapValues = methods.MapValues?.CreateDelegate<Action<IEventValueMapBuilder<TUpgrade, TPrevious>>>(migration);
    }

    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<TUpgrade, TPrevious> builder) => _upcast(builder);

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<TPrevious, TUpgrade> builder) => _downcast(builder);

    /// <inheritdoc/>
    public override void MapValues(IEventValueMapBuilder<TUpgrade, TPrevious> builder) => _mapValues?.Invoke(builder);
}
