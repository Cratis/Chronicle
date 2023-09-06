// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a unique identifier for a job.
/// </summary>
/// <param name="Value">The actual value.</param>
public record JobId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="JobId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert from.</param>
    public static implicit operator JobId(Guid value) => new(value);

    /// <summary>
    /// Create a new unique <see cref="JobId"/>.
    /// </summary>
    /// <returns><see cref="JobId"/> created.</returns>
    public static JobId New() => new(Guid.NewGuid());
}
