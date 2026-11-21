// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the client API for the policy of hosted federation subjects: what each subject may do and where its events go, and the
/// live state of its link. It is for the operator of the kernel that accepts the links.
/// </summary>
/// <remarks>
/// <para>
/// Get it from <see cref="IChronicleClient.FederationPolicies"/>. The kernel the client is connected to is the kernel that is acted on,
/// and every member needs administrator rights on it.
/// </para>
/// <para>
/// A failed command throws, so a policy that was not changed is never reported as changed. A suspended policy refuses its links at once
/// and can be resumed. A revoked direction is refused until it is set again; a revoked policy is permanent and the subject has to be
/// given a new id to be admitted again.
/// </para>
/// </remarks>
public interface IFederationPolicies
{
    /// <summary>
    /// Set what a subject may do in one direction, creating the policy of the subject when it has none. The other direction is left as it is.
    /// </summary>
    /// <param name="subject">The opaque subject the policy is for, the customer part of the peer's identity.</param>
    /// <param name="entry">The entry to set: the direction, the target event store and namespace, and the event types allowed.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Set(string subject, FederationPolicyEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspend the policy of a subject, so its links are refused at once. Other subjects are not affected.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Suspend(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume a suspended policy of a subject.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Resume(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke the policy of a subject permanently, so its links are refused at once.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Revoke(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke one direction of the policy of a subject, so links in that direction are refused at once while the other direction keeps running.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="direction">The direction to revoke.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task RevokeDirection(string subject, FederationDirection direction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the policies of every subject.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The policies.</returns>
    Task<IEnumerable<FederationPolicyInformation>> GetPolicies(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the live state of the link of every subject that has a policy.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The link state of each subject.</returns>
    Task<IEnumerable<FederationLinkInformation>> GetLinks(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the live state of the link of one subject.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The link state, or null when the subject has no policy.</returns>
    Task<FederationLinkInformation?> GetLink(string subject, CancellationToken cancellationToken = default);
}
