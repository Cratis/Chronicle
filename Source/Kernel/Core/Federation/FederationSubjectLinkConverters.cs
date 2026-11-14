// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Builds <see cref="FederationSubjectLink"/> from the live state of the links.
/// </summary>
/// <remarks>
/// Kept apart from the read model so the helper does not become a query of its own.
/// </remarks>
public static class FederationSubjectLinkConverters
{
    /// <summary>
    /// Gets the link state of a subject.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="status">The <see cref="IFederationStatus"/> holding the live state of the links.</param>
    /// <returns>The link state.</returns>
    public static FederationSubjectLink ToSubjectLink(string subject, IFederationStatus status)
    {
        var outbound = status.GetLink($"subject:{subject}");
        var inboundLinkId = FederationServeService.LogicalSourceOf(subject);
        var inbound = status.GetInbound(inboundLinkId);
        var binding = status.GetBinding(inboundLinkId);
        var lastActivity = LatestOf(outbound.LastPull, inbound.LastReceived);

        return new(
            subject,
            status.HasSession(subject),
            lastActivity,
            outbound.LastPull,
            outbound.EventsDelivered,
            outbound.BytesDelivered,
            outbound.Throttled,
            binding == FederationBindingStatus.Unknown ? string.Empty : binding.State,
            inbound.LastReceived,
            inbound.EventsReceived,
            inbound.BytesReceived,
            binding.Lag,
            binding.LastError ?? string.Empty);
    }

    static DateTimeOffset? LatestOf(DateTimeOffset? first, DateTimeOffset? second)
    {
        if (first is null || second is null)
        {
            return first ?? second;
        }

        return first > second ? first : second;
    }
}
