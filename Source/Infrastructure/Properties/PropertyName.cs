// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Represents a <see cref="IPropertyPathSegment"/> for a property name.
/// </summary>
/// <param name="Value">Name of the property.</param>
public record PropertyName(string Value) : IPropertyPathSegment
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
