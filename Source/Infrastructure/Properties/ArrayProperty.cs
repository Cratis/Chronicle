// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Represents a <see cref="IPropertyPathSegment"/> for an array.
/// </summary>
/// <param name="Value">Identifier of the array indexer.</param>
public record ArrayProperty(string Value) : IPropertyPathSegment
{
    /// <inheritdoc/>
    public override string ToString() => $"[{Value}]";
}
