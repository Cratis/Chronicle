// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Represents the unique identifier of a specific store configuration using in a projection pipeline.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record SinkConfigurationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the none representation of <see cref="SinkConfigurationId"/>.
    /// </summary>
    public static readonly SinkConfigurationId None = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="SinkConfigurationId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> to convert from.</param>
    /// <returns>Converted <see cref="SinkConfigurationId"/> instance.</returns>
    public static implicit operator SinkConfigurationId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> representation of <see cref="Guid"/> to <see cref="SinkConfigurationId"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> representation of <see cref="Guid"/>.</param>
    /// <returns>Converted <see cref="SinkConfigurationId"/> instance.</returns>
    public static implicit operator SinkConfigurationId(string value) => new(Guid.Parse(value));
}
