// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents how long a superseded webhook signing key stays valid after a rotation.
/// </summary>
/// <param name="Value">The actual duration.</param>
public record WebhookSigningKeyOverlap(TimeSpan Value) : ConceptAs<TimeSpan>(Value)
{
    /// <summary>
    /// Gets the default overlap of 24 hours.
    /// </summary>
    public static readonly WebhookSigningKeyOverlap Default = new(TimeSpan.FromHours(24));

    /// <summary>
    /// Gets an overlap of none, retiring the superseded key immediately.
    /// </summary>
    public static readonly WebhookSigningKeyOverlap None = new(TimeSpan.Zero);

    /// <summary>
    /// Gets the maximum overlap of 30 days.
    /// </summary>
    public static readonly WebhookSigningKeyOverlap Maximum = new(TimeSpan.FromDays(30));

    /// <summary>
    /// Implicitly convert from a <see cref="TimeSpan"/> to <see cref="WebhookSigningKeyOverlap"/>.
    /// </summary>
    /// <param name="value">The duration to convert from.</param>
    public static implicit operator WebhookSigningKeyOverlap(TimeSpan value) => new(value);
}
