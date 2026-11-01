// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Represents owner-wide HTTP delivery settings within one event store.
/// </summary>
/// <param name="Owner">The program identifier.</param>
/// <param name="OwnerPrincipal">The authenticated owner principal.</param>
public record ObserverOwnerSettings(ProgramIdentifier Owner, OwnerPrincipal OwnerPrincipal)
{
    /// <summary>
    /// Gets the HTTP delivery timeout, including for legacy payloads without this field.
    /// </summary>
    public TimeSpan HttpDeliveryTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the settings compare-and-set revision, independent of every observer revision.
    /// </summary>
    public ulong Revision { get; init; }

    /// <summary>
    /// Gets whether the timeout is positive and representable by a platform timer.
    /// </summary>
    [JsonIgnore]
    public bool HasValidTimeout => HttpDeliveryTimeout > TimeSpan.Zero && HttpDeliveryTimeout.TotalMilliseconds <= uint.MaxValue - 1;
}
