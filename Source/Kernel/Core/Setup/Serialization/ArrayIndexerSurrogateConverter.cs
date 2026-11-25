// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between an <see cref="ArrayIndexer"/> and its <see cref="ArrayIndexerSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class ArrayIndexerSurrogateConverter : IConverter<ArrayIndexer, ArrayIndexerSurrogate>
{
    /// <inheritdoc/>
    public ArrayIndexer ConvertFromSurrogate(in ArrayIndexerSurrogate surrogate) => new(surrogate.ArrayProperty, surrogate.IdentifierProperty, surrogate.Identifier);

    /// <inheritdoc/>
    public ArrayIndexerSurrogate ConvertToSurrogate(in ArrayIndexer value) => new(value.ArrayProperty, value.IdentifierProperty, value.Identifier);
}
