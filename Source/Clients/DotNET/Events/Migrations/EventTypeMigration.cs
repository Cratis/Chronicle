// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Abstract base class for type-safe event type migrations between two generations.
/// Extracts <see cref="IEventTypeMigration.From"/> and <see cref="IEventTypeMigration.To"/>
/// from the <see cref="EventTypeAttribute"/> on <typeparamref name="TUpgrade"/> and
/// <typeparamref name="TPrevious"/>, and validates that the upgrade generation is exactly
/// one more than the previous generation.
/// </summary>
/// <typeparam name="TUpgrade">The upgraded (newer generation) event type.</typeparam>
/// <typeparam name="TPrevious">The previous (older generation) event type.</typeparam>
public abstract class EventTypeMigration<TUpgrade, TPrevious> : IEventTypeMigrationFor<TUpgrade, TPrevious>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeMigration{TUpgrade, TPrevious}"/> class.
    /// </summary>
    /// <exception cref="InvalidMigrationGenerationGap">
    /// Thrown when <typeparamref name="TUpgrade"/>'s generation is not exactly one more
    /// than <typeparamref name="TPrevious"/>'s generation.
    /// </exception>
    protected EventTypeMigration()
    {
        var previousEventType = typeof(TPrevious).GetEventType();
        var upgradeEventType = typeof(TUpgrade).GetEventType();

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

    /// <inheritdoc/>
    void IEventTypeMigration.Upcast(IEventMigrationBuilder builder)
    {
        var typedBuilder = new EventMigrationBuilderFor<TUpgrade, TPrevious>(builder);
        Upcast(typedBuilder);
    }

    /// <inheritdoc/>
    void IEventTypeMigration.Downcast(IEventMigrationBuilder builder)
    {
        var typedBuilder = new EventMigrationBuilderFor<TPrevious, TUpgrade>(builder);
        Downcast(typedBuilder);
    }
}
