// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// Represents the name of a sink configuration in the kernel's <c language="text">sinks</c> configuration.
/// </summary>
/// <param name="Value">Underlying value.</param>
/// <remarks>
/// An empty name means the default sink for the kind of read model: the configured <c language="text">sinks.readModels</c> or
/// <c language="text">sinks.graphs</c> configuration, or the implicit default when none is configured.
/// </remarks>
public record SinkName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the name that means the default sink for the kind of read model.
    /// </summary>
    public static readonly SinkName Default = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the name is the default sink for the kind of read model.
    /// </summary>
    public bool IsDefault => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SinkName"/>.
    /// </summary>
    /// <param name="value">String value to convert from.</param>
    public static implicit operator SinkName(string value) => new(value);
}
