// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="Capture"/> and its <see cref="CaptureSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class CaptureSurrogateConverter : IConverter<Capture, CaptureSurrogate>
{
    /// <inheritdoc/>
    public Capture ConvertFromSurrogate(in CaptureSurrogate surrogate) => new(surrogate.Id, surrogate.Name, surrogate.Declaration, surrogate.Status);

    /// <inheritdoc/>
    public CaptureSurrogate ConvertToSurrogate(in Capture value) => new(value.Id, value.Name, value.Declaration, value.Status);
}
