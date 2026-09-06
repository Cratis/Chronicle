// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Abstract base class for type-safe event type migrations between two generations.
/// Extracts <see cref="IEventTypeMigration.From"/> and <see cref="IEventTypeMigration.To"/>
/// from <typeparamref name="TUpgrade"/> and <typeparamref name="TPrevious"/> - each resolved via
/// <see cref="EventTypeAttribute"/> or <see cref="EventTypeGenerationForAttribute{TEventType}"/> -
/// validates that both resolve to the same event type id, and that the upgrade generation is
/// exactly one more than the previous generation.
/// </summary>
/// <typeparam name="TUpgrade">The upgraded (newer generation) event type.</typeparam>
/// <typeparam name="TPrevious">The previous (older generation) event type.</typeparam>
public abstract class EventTypeMigration<TUpgrade, TPrevious> : IEventTypeMigrationFor<TUpgrade, TPrevious>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeMigration{TUpgrade, TPrevious}"/> class.
    /// </summary>
    /// <exception cref="MigrationGenerationsMustShareEventTypeId">
    /// Thrown when <typeparamref name="TUpgrade"/> and <typeparamref name="TPrevious"/> resolve to
    /// different event type ids.
    /// </exception>
    /// <exception cref="InvalidMigrationGenerationGap">
    /// Thrown when <typeparamref name="TUpgrade"/>'s generation is not exactly one more
    /// than <typeparamref name="TPrevious"/>'s generation.
    /// </exception>
    protected EventTypeMigration()
    {
        var previousEventType = typeof(TPrevious).GetEventType();
        var upgradeEventType = typeof(TUpgrade).GetEventType();

        if (previousEventType.Id != upgradeEventType.Id)
        {
            throw new MigrationGenerationsMustShareEventTypeId(typeof(TPrevious), typeof(TUpgrade), previousEventType.Id, upgradeEventType.Id);
        }

        From = previousEventType.Generation;
        To = upgradeEventType.Generation;

        if (To.Value != From.Value + 1)
        {
            throw new InvalidMigrationGenerationGap(typeof(TPrevious), typeof(TUpgrade), From, To);
        }
    }

    /// <inheritdoc/>
    public EventTypeGeneration From { get; }

    /// <inheritdoc/>
    public EventTypeGeneration To { get; }

    /// <summary>
    /// Define the type-safe upcast migration from <typeparamref name="TPrevious"/> to <typeparamref name="TUpgrade"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationBuilder{TUpgrade, TPrevious}"/> to use.</param>
    public abstract void Upcast(IEventMigrationBuilder<TUpgrade, TPrevious> builder);

    /// <summary>
    /// Define the type-safe downcast migration from <typeparamref name="TUpgrade"/> to <typeparamref name="TPrevious"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationBuilder{TPrevious, TUpgrade}"/> to use.</param>
    public abstract void Downcast(IEventMigrationBuilder<TPrevious, TUpgrade> builder);

    /// <summary>
    /// Declare the values that keep their property but change what they mean between the two generations.
    /// </summary>
    /// <param name="builder">The <see cref="IEventValueMapBuilder{TUpgrade, TPrevious}"/> to use.</param>
    /// <remarks>
    /// A map declared here is applied in both directions - forward when upcasting, inverted when downcasting - so an
    /// enum member that was renumbered, or a code that took on a new spelling, is stated once instead of twice. It is
    /// applied before <see cref="Upcast"/> and <see cref="Downcast"/> run, so a migration that needs a different
    /// answer for one direction states that direction's transformation itself and it wins.
    /// </remarks>
    public virtual void MapValues(IEventValueMapBuilder<TUpgrade, TPrevious> builder)
    {
    }

    /// <inheritdoc/>
    void IEventTypeMigration.Upcast(IEventMigrationBuilder builder)
    {
        ApplyValueMaps(builder, (maps, properties) => maps.ApplyUpcast(properties));
        Upcast(new EventMigrationBuilderFor<TUpgrade, TPrevious>(builder));
    }

    /// <inheritdoc/>
    void IEventTypeMigration.Downcast(IEventMigrationBuilder builder)
    {
        ApplyValueMaps(builder, (maps, properties) => maps.ApplyDowncast(properties));
        Downcast(new EventMigrationBuilderFor<TPrevious, TUpgrade>(builder));
    }

    void ApplyValueMaps(IEventMigrationBuilder builder, Action<EventValueMapBuilder<TUpgrade, TPrevious>, IEventMigrationPropertyBuilder> apply)
    {
        var maps = new EventValueMapBuilder<TUpgrade, TPrevious>();
        MapValues(maps);

        if (!maps.HasMaps)
        {
            return;
        }

        builder.Properties(properties => apply(maps, properties));
    }
}
