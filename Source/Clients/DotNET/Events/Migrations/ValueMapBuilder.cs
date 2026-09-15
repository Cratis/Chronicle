// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IValueMapBuilder{TFrom, TTo}"/>.
/// </summary>
/// <typeparam name="TFrom">The type of the value in the source generation.</typeparam>
/// <typeparam name="TTo">The type of the value in the target generation.</typeparam>
public class ValueMapBuilder<TFrom, TTo> : IValueMapBuilder<TFrom, TTo>
{
    readonly List<ValueMapping> _mappings = [];

    /// <summary>
    /// Gets the mappings that have been declared.
    /// </summary>
    public IEnumerable<ValueMapping> Mappings => _mappings;

    /// <inheritdoc/>
    public IValueMapBuilder<TFrom, TTo> Map(TFrom from, TTo to)
    {
        _mappings.Add(new ValueMapping(from!, to!));
        return this;
    }
}
