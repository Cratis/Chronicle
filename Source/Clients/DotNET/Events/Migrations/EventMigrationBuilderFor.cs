// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationBuilder{TTarget, TSource}"/>
/// that delegates to the underlying <see cref="IEventMigrationBuilder"/>.
/// </summary>
/// <typeparam name="TTarget">The target event type of the migration.</typeparam>
/// <typeparam name="TSource">The source event type of the migration.</typeparam>
/// <param name="inner">The underlying <see cref="IEventMigrationBuilder"/> to delegate to.</param>
public class EventMigrationBuilderFor<TTarget, TSource>(IEventMigrationBuilder inner) : IEventMigrationBuilder<TTarget, TSource>
{
    /// <inheritdoc/>
    public void Properties(Action<IEventMigrationPropertyBuilder<TTarget, TSource>> properties)
    {
        inner.Properties(untyped =>
        {
            var typed = new EventMigrationPropertyBuilderFor<TTarget, TSource>(untyped);
            properties(typed);
        });
    }
}
