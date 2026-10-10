// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Performs bounded, payload-free metadata reads and uncached identity resolution.
/// </summary>
internal static class EventMetadataQuerying
{
    /// <summary>
    /// The maximum number of requested locators, before deduplication.
    /// </summary>
    internal const int MaxLocators = 500;

    /// <summary>
    /// Reads metadata and resolves identity names directly from storage.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The sequence.</param>
    /// <param name="sequenceNumbers">The requested locators.</param>
    /// <returns>The matching metadata.</returns>
    /// <exception cref="TooManyEventLocators">More than 500 locators were supplied.</exception>
    internal static async Task<IEnumerable<EventMetadata>> Read(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        IEnumerable<ulong> sequenceNumbers)
    {
        var locators = sequenceNumbers.Take(MaxLocators + 1).ToArray();
        if (locators.Length > MaxLocators)
        {
            throw new TooManyEventLocators();
        }
        if (locators.Length == 0)
        {
            return [];
        }
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var metadata = await namespaceStorage.GetEventSequence(eventSequenceId).GetMetadataAt(locators.Distinct().Select(_ => new EventSequenceNumber(_)));
        var ids = metadata.SelectMany(_ => _.CausedByChain).Distinct().ToArray();
        var identities = await namespaceStorage.Identities.GetByIds(ids);
        return metadata.Select(entry =>
        {
            var causedBy = Resolve(entry.CausedByChain, identities);
            return new EventMetadata(
                entry.SequenceNumber,
                entry.EventTypeId,
                entry.EventSourceType,
                entry.EventSourceId,
                entry.EventStreamType,
                entry.EventStreamId,
                entry.Occurred,
                entry.CorrelationId,
                entry.Causation.Select(cause => new Causation(cause.Occurred, cause.Type.Value, cause.Properties)).ToArray(),
                causedBy,
                KindOf(causedBy),
                entry.Tags,
                entry.Subject,
                entry.EventSourceName);
        }).ToArray();
    }

    static ResolvedIdentity Resolve(IEnumerable<IdentityId> chain, IReadOnlyDictionary<IdentityId, Concepts.Identities.Identity> identities)
    {
        ResolvedIdentity? next = null;
        foreach (var id in chain.Reverse())
        {
            if (id == IdentityId.NotSet)
            {
                next = new(Concepts.Identities.Identity.NotSet.Subject, null, Concepts.Identities.Identity.NotSet.UserName, IdentityResolution.NotSet, next);
                continue;
            }
            if (!identities.TryGetValue(id, out var identity))
            {
                next = new(string.Empty, null, string.Empty, IdentityResolution.Missing, next);
                continue;
            }
            var resolution = IdentityResolution.Resolved;
            if (identity.Subject == Concepts.Identities.Identity.System.Subject)
            {
                resolution = IdentityResolution.System;
            }
            else if (identity.Subject == Concepts.Identities.Identity.NotSet.Subject)
            {
                resolution = IdentityResolution.NotSet;
            }
            else if (string.IsNullOrEmpty(identity.Name))
            {
                resolution = IdentityResolution.NameUnavailable;
            }
            next = new(identity.Subject, resolution == IdentityResolution.Resolved ? identity.Name : null, identity.UserName, resolution, next);
        }
        return next ?? new ResolvedIdentity(Concepts.Identities.Identity.NotSet.Subject, null, Concepts.Identities.Identity.NotSet.UserName, IdentityResolution.NotSet, null);
    }

    static InitiatorType KindOf(ResolvedIdentity identity)
    {
        if (identity.Subject == Concepts.Identities.Identity.Unknown.Subject)
        {
            return InitiatorType.Unknown;
        }
        return identity.Resolution switch
        {
            IdentityResolution.System => InitiatorType.System,
            IdentityResolution.Missing or IdentityResolution.NotSet => InitiatorType.Unknown,
            _ => identity.OnBehalfOf is not null ? InitiatorType.Agent : InitiatorType.User
        };
    }
}
