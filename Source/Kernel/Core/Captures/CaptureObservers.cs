// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Holds the identities an events capture is observed and remembered under.
/// </summary>
public static class CaptureObservers
{
    /// <summary>
    /// The prefix of the observer identifier of every events capture.
    /// </summary>
    public const string ObserverIdPrefix = "$system.captures.";

    /// <summary>
    /// Gets the <see cref="ObserverId"/> an events capture observes its inbox under.
    /// </summary>
    /// <param name="captureId">The <see cref="CaptureId"/> of the capture.</param>
    /// <returns>The <see cref="ObserverId"/>.</returns>
    public static ObserverId For(CaptureId captureId) => $"{ObserverIdPrefix}{captureId.Value:N}";

    /// <summary>
    /// Try to get the <see cref="CaptureId"/> from the <see cref="ObserverId"/> of an events capture.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <param name="captureId">The resulting <see cref="CaptureId"/>.</param>
    /// <returns>True when the observer belongs to an events capture, false when not.</returns>
    public static bool TryGetCaptureId(ObserverId observerId, out CaptureId captureId)
    {
        captureId = CaptureId.NotSet;
        var value = observerId.Value;
        if (!value.StartsWith(ObserverIdPrefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(value[ObserverIdPrefix.Length..], "N", out var guid))
        {
            return false;
        }

        captureId = guid;
        return true;
    }

    /// <summary>
    /// Gets the identity the state of an events capture is remembered under for one namespace. Every namespace
    /// of an event store has its own state, so what one tenant's events said never decides what another's produce.
    /// </summary>
    /// <param name="captureId">The <see cref="CaptureId"/> of the capture.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the events arrive in.</param>
    /// <returns>The <see cref="CaptureId"/> to store the observation under.</returns>
    public static CaptureId ObservationIdFor(CaptureId captureId, EventStoreNamespaceName @namespace)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{captureId.Value:N}:{@namespace.Value}"));
        return new CaptureId(new Guid(hash.AsSpan(0, 16)));
    }
}
