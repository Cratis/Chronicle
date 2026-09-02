// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventValueMapBuilder{TUpgrade, TPrevious}"/>.
/// </summary>
/// <typeparam name="TUpgrade">The upgraded (newer generation) event type.</typeparam>
/// <typeparam name="TPrevious">The previous (older generation) event type.</typeparam>
public class EventValueMapBuilder<TUpgrade, TPrevious> : IEventValueMapBuilder<TUpgrade, TPrevious>
{
    readonly List<PropertyValueMap> _maps = [];

    /// <summary>
    /// Gets whether any value map has been declared.
    /// </summary>
    public bool HasMaps => _maps.Count > 0;

    /// <inheritdoc/>
    public IEventValueMapBuilder<TUpgrade, TPrevious> For<TUpgradeProperty, TPreviousProperty>(
        Expression<Func<TUpgrade, TUpgradeProperty>> upgradeProperty,
        Expression<Func<TPrevious, TPreviousProperty>> previousProperty,
        Action<IValueMapBuilder<TPreviousProperty, TUpgradeProperty>> map)
    {
        var mapBuilder = new ValueMapBuilder<TPreviousProperty, TUpgradeProperty>();
        map(mapBuilder);

        _maps.Add(new PropertyValueMap(
            new PropertyName(upgradeProperty.GetPropertyPath()),
            new PropertyName(previousProperty.GetPropertyPath()),
            [.. mapBuilder.Mappings]));

        return this;
    }

    /// <summary>
    /// Apply every declared map to the upcast of the migration, translating previous values into upgraded ones.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationPropertyBuilder"/> to apply to.</param>
    public void ApplyUpcast(IEventMigrationPropertyBuilder builder) =>
        _maps.ForEach(map => builder.MapValues(map.UpgradeProperty, map.PreviousProperty, map.Mappings));

    /// <summary>
    /// Apply every declared map to the downcast of the migration, translating upgraded values back into previous ones.
    /// </summary>
    /// <param name="builder">The <see cref="IEventMigrationPropertyBuilder"/> to apply to.</param>
    public void ApplyDowncast(IEventMigrationPropertyBuilder builder) =>
        _maps.ForEach(map => builder.MapValues(map.PreviousProperty, map.UpgradeProperty, Invert(map.Mappings)));

    /// <summary>
    /// Inverts a map so it translates the other way.
    /// </summary>
    /// <param name="mappings">The mappings to invert.</param>
    /// <returns>The inverted mappings.</returns>
    /// <remarks>
    /// Several source values collapsing onto one target value have no single inverse, so the first pair declared for
    /// a target value is the one that gets to represent it going back. A migration that needs a different answer
    /// states the reverse map itself in its <c>Downcast</c>, which overrides what is derived here.
    /// </remarks>
    static ValueMapping[] Invert(IEnumerable<ValueMapping> mappings) =>
        [.. mappings
            .GroupBy(mapping => mapping.To)
            .Select(group => new ValueMapping(group.Key, group.First().From))];

    /// <summary>
    /// Represents one declared value map, tying the two generations' properties to the values that change meaning.
    /// </summary>
    /// <param name="UpgradeProperty">The property on the upgraded generation.</param>
    /// <param name="PreviousProperty">The property on the previous generation.</param>
    /// <param name="Mappings">The values that change meaning, expressed from previous to upgraded.</param>
    sealed record PropertyValueMap(PropertyName UpgradeProperty, PropertyName PreviousProperty, ValueMapping[] Mappings);
}
