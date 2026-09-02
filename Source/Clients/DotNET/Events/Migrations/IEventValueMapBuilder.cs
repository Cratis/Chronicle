// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a builder for the value maps of an event type migration - the values that keep their property but change
/// what they mean between two generations.
/// </summary>
/// <typeparam name="TUpgrade">The upgraded (newer generation) event type.</typeparam>
/// <typeparam name="TPrevious">The previous (older generation) event type.</typeparam>
/// <remarks>
/// A map declared here is stated once and applied in both directions: forward when upcasting, inverted when
/// downcasting. That is the whole point of declaring it as a map rather than as two transformations - the pair of
/// values is one fact about the two generations, and stating it twice is how the two directions drift apart. A
/// direction that states its own transformation for the same property keeps it.
/// </remarks>
public interface IEventValueMapBuilder<TUpgrade, TPrevious>
{
    /// <summary>
    /// Declare the value map for a property, from how the previous generation expressed it to how the upgraded
    /// generation does.
    /// </summary>
    /// <typeparam name="TUpgradeProperty">The type of the property on the upgraded generation.</typeparam>
    /// <typeparam name="TPreviousProperty">The type of the property on the previous generation.</typeparam>
    /// <param name="upgradeProperty">Expression selecting the property on the upgraded generation.</param>
    /// <param name="previousProperty">Expression selecting the property on the previous generation.</param>
    /// <param name="map">Action declaring which value becomes which.</param>
    /// <returns>The builder for continued configuration.</returns>
    /// <remarks>
    /// The two properties do not have to share a name - the map carries the value across from one to the other, so it
    /// also covers a rename of the property it maps.
    /// </remarks>
    IEventValueMapBuilder<TUpgrade, TPrevious> For<TUpgradeProperty, TPreviousProperty>(
        Expression<Func<TUpgrade, TUpgradeProperty>> upgradeProperty,
        Expression<Func<TPrevious, TPreviousProperty>> previousProperty,
        Action<IValueMapBuilder<TPreviousProperty, TUpgradeProperty>> map);
}
