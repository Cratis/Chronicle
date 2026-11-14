// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the operator-side policy of hosted federation, keyed by the opaque subject in a peer's identity.
/// </summary>
/// <remarks>
/// Every change is an event in the system event log carrying the identity that acted, and takes effect on the subject's links
/// at once. A change to one subject never affects another.
/// </remarks>
public interface IFederationPolicies
{
    /// <summary>
    /// Set what a subject may do in one direction. The first call creates the policy.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="direction">The direction the entry governs.</param>
    /// <param name="eventStore">The local event store the subject's link addresses.</param>
    /// <param name="namespace">The namespace of that event store the subject's link addresses.</param>
    /// <param name="allPublic">Whether the entry covers every public event type.</param>
    /// <param name="eventTypes">The specific event types allowed when <paramref name="allPublic"/> is false.</param>
    /// <param name="actedBy">The identity that acts.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidFederationPolicy">Thrown when the policy is not valid, or the subject's policy is revoked.</exception>
    Task Set(string subject, FederationDirection direction, string eventStore, string @namespace, bool allPublic, IEnumerable<string> eventTypes, string actedBy);

    /// <summary>
    /// Suspend the policy of a subject. Suspending a suspended policy changes nothing.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="actedBy">The identity that acts.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidFederationPolicy">Thrown when the subject has no policy or it is revoked.</exception>
    Task Suspend(string subject, string actedBy);

    /// <summary>
    /// Resume the suspended policy of a subject. Resuming an active policy changes nothing.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="actedBy">The identity that acts.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidFederationPolicy">Thrown when the subject has no policy or it is revoked.</exception>
    Task Resume(string subject, string actedBy);

    /// <summary>
    /// Revoke the policy of a subject for good. Revoking a revoked policy changes nothing.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="actedBy">The identity that acts.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidFederationPolicy">Thrown when the subject has no policy.</exception>
    Task Revoke(string subject, string actedBy);
}
