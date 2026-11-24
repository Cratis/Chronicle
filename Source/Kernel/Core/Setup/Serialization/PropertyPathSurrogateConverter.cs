// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="PropertyPath"/> and its <see cref="PropertyPathSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class PropertyPathSurrogateConverter : IConverter<PropertyPath, PropertyPathSurrogate>
{
    /// <inheritdoc/>
    public PropertyPath ConvertFromSurrogate(in PropertyPathSurrogate surrogate) => new(surrogate.Path);

    /// <inheritdoc/>
    public PropertyPathSurrogate ConvertToSurrogate(in PropertyPath value) => new(value.Path);
}
