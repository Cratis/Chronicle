// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Represents a <see cref="IPropertyPathSegment"/> for a *THIS* property.
/// </summary>
/// <remarks>
/// THIS or $this properties represents the actual instance of a value.
/// </remarks>
public record ThisAccessor() : IPropertyPathSegment
{
    /// <inheritdoc/>
    public string Value => "$this";

    /// <inheritdoc/>
    public override string ToString() => "$this";
}
