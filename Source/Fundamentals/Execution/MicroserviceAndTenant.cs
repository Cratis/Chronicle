// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents a key combination of <see cref="MicroserviceId"/> and <see cref="TenantId"/>.
/// </summary>
/// <param name="MicroserviceId">The <see cref="MicroserviceId"/>.</param>
/// <param name="TenantId">The <see cref="TenantId"/>.</param>
public record MicroserviceAndTenant(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a string representation of <see cref="MicroserviceAndTenant"/>.
    /// </summary>
    /// <param name="input">String to parse.</param>
    /// <returns>Parsed version.</returns>
    public static MicroserviceAndTenant Parse(string input)
    {
        var segments = input.Split('+');
        return new(segments[0], segments[1]);
    }
}
