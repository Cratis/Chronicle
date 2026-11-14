// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationPolicies"/> that records every change as an event in the system event log.
/// </summary>
/// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding and recording the policies.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class FederationPolicies(IFederationCertificateRegistry registry, ILogger<FederationPolicies> logger) : IFederationPolicies
{
    /// <inheritdoc/>
    public async Task Set(string subject, FederationDirection direction, string eventStore, string @namespace, bool allPublic, IEnumerable<string> eventTypes, string actedBy)
    {
        var types = (eventTypes ?? []).Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (!FederationIdentities.IsValidSegment(subject))
        {
            throw new InvalidFederationPolicy(subject ?? string.Empty, "the subject must be 1 to 64 letters, digits, dots, hyphens or underscores");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new InvalidFederationPolicy(subject, "the direction is unknown");
        }

        if (string.IsNullOrWhiteSpace(eventStore) || string.IsNullOrWhiteSpace(@namespace))
        {
            throw new InvalidFederationPolicy(subject, "the target event store and namespace are required");
        }

        if (!allPublic && types.Length == 0)
        {
            throw new InvalidFederationPolicy(subject, "list the allowed event types, or allow all public event types");
        }

        var existing = await registry.FindPolicy(subject);
        if (existing?.Status == FederationPolicyStatus.Revoked)
        {
            throw new InvalidFederationPolicy(subject, "the policy is revoked. Use a new subject");
        }

        await registry.Append(subject, new FederationPolicySet(direction, eventStore, @namespace, allPublic, allPublic ? [] : types, actedBy));
        logger.PolicySet(subject, direction);
    }

    /// <inheritdoc/>
    public async Task Suspend(string subject, string actedBy)
    {
        var existing = await Existing(subject, allowRevoked: false);
        if (existing.Status == FederationPolicyStatus.Suspended)
        {
            return;
        }

        await registry.Append(subject, new FederationPolicySuspended(actedBy));
        logger.PolicySuspended(subject);
    }

    /// <inheritdoc/>
    public async Task Resume(string subject, string actedBy)
    {
        var existing = await Existing(subject, allowRevoked: false);
        if (existing.Status == FederationPolicyStatus.Active)
        {
            return;
        }

        await registry.Append(subject, new FederationPolicyResumed(actedBy));
        logger.PolicyResumed(subject);
    }

    /// <inheritdoc/>
    public async Task Revoke(string subject, string actedBy)
    {
        var existing = await Existing(subject, allowRevoked: true);
        if (existing.Status == FederationPolicyStatus.Revoked)
        {
            return;
        }

        await registry.Append(subject, new FederationPolicyRevoked(actedBy));
        logger.PolicyRevoked(subject);
    }

    async Task<FederationPolicyState> Existing(string subject, bool allowRevoked)
    {
        subject ??= string.Empty;
        var existing = await registry.FindPolicy(subject)
            ?? throw new InvalidFederationPolicy(subject, "there is no policy for the subject");
        return existing.Status == FederationPolicyStatus.Revoked && !allowRevoked
            ? throw new InvalidFederationPolicy(subject, "the policy is revoked")
            : existing;
    }
}
