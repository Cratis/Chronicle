// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a type-safe builder for event migrations between two event type generations.
/// </summary>
/// <typeparam name="TTarget">The target event type of the migration.</typeparam>
/// <typeparam name="TSource">The source event type of the migration.</typeparam>
public interface IEventMigrationBuilder<TTarget, TSource>
{
    /// <summary>
    /// Define property migrations using expression-based property accessors.
    /// </summary>
    /// <param name="properties">Action to configure properties.</param>
    void Properties(Action<IEventMigrationPropertyBuilder<TTarget, TSource>> properties);
}
