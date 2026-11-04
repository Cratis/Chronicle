// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the event stores a cluster-wide operation can reach, and the quarantined ones it had to leave out.
/// </summary>
/// <param name="Usable">The event stores that are not quarantined.</param>
/// <param name="Skipped">The quarantined event stores that were left out.</param>
/// <remarks>
/// A quarantined event store refuses to be opened, so an operation that fans out across every event store (patches,
/// erasure) must not try to open it: that would fail the whole operation, and for startup patches it would stop the
/// silo from starting. Leaving the store out is only half of the rule - the caller decides how the operation reaches
/// the skipped store once the quarantine is resolved.
/// </remarks>
public record UsableEventStores(IReadOnlyList<EventStoreName> Usable, IReadOnlyList<EventStoreName> Skipped)
{
    /// <summary>
    /// Gets whether any event store was left out.
    /// </summary>
    public bool HasSkipped => Skipped.Count > 0;

    /// <summary>
    /// Lists the event stores of the installation, leaving out the quarantined ones and logging a warning for each.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to list.</param>
    /// <param name="resourceNames">The optional <see cref="IResourceNameSnapshot"/>. Without one, nothing is quarantined.</param>
    /// <param name="logger">The <see cref="ILogger"/> the skipped stores are reported to.</param>
    /// <returns>The <see cref="UsableEventStores"/>.</returns>
    public static async Task<UsableEventStores> From(IStorage storage, IResourceNameSnapshot? resourceNames, ILogger logger)
    {
        var usable = new List<EventStoreName>();
        var skipped = new List<EventStoreName>();
        foreach (var eventStore in await storage.GetEventStores())
        {
            if (resourceNames?.IsQuarantined(eventStore) == true)
            {
                logger.LeavingOutQuarantinedEventStore(ResourceNameText.Escape(eventStore.Value));
                skipped.Add(eventStore);
                continue;
            }

            usable.Add(eventStore);
        }

        return new(usable, skipped);
    }
}
