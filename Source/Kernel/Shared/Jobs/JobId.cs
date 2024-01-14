// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Jobs;

/// <summary>
/// Represents a unique identifier for a job.
/// </summary>
/// <param name="Value">The actual value.</param>
public record JobId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents the identifier for a job identifier not set.
    /// </summary>
    public static readonly JobId NotSet = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="JobId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert from.</param>
    public static implicit operator JobId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="JobId"/>.
    /// </summary>
    /// <param name="value">String representation of a <see cref="Guid"/>.</param>
    public static implicit operator JobId(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Create a new unique <see cref="JobId"/>.
    /// </summary>
    /// <returns><see cref="JobId"/> created.</returns>
    public static JobId New() => new(Guid.NewGuid());
}
