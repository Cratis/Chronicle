// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between <see cref="ArrayIndexers"/> and its <see cref="ArrayIndexersSurrogate"/>.
/// </summary>
/// <remarks>
/// Keys without indexers are by far the common case, so they come back as the shared <see cref="ArrayIndexers.NoIndexers"/>.
/// </remarks>
[RegisterConverter]
public sealed class ArrayIndexersSurrogateConverter : IConverter<ArrayIndexers, ArrayIndexersSurrogate>
{
    /// <inheritdoc/>
    public ArrayIndexers ConvertFromSurrogate(in ArrayIndexersSurrogate surrogate) =>
        surrogate.All is { Count: > 0 } ? new ArrayIndexers(surrogate.All) : ArrayIndexers.NoIndexers;

    /// <inheritdoc/>
    public ArrayIndexersSurrogate ConvertToSurrogate(in ArrayIndexers value) => new([.. value.All]);
}
