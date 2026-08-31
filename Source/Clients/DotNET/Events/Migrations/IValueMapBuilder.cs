// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a builder for the individual values of a property that change meaning between two event type generations.
/// </summary>
/// <typeparam name="TFrom">The type of the value in the source generation.</typeparam>
/// <typeparam name="TTo">The type of the value in the target generation.</typeparam>
public interface IValueMapBuilder<TFrom, TTo>
{
    /// <summary>
    /// State that a value in the source generation becomes a specific value in the target generation.
    /// </summary>
    /// <param name="from">The value as it appears in the source generation.</param>
    /// <param name="to">The value it becomes in the target generation.</param>
    /// <returns>The builder for continued configuration.</returns>
    /// <remarks>
    /// Only values the map mentions are translated - anything else is carried across as it is, because a value the
    /// map stays silent about means the same thing in both generations.
    /// </remarks>
    IValueMapBuilder<TFrom, TTo> Map(TFrom from, TTo to);
}
