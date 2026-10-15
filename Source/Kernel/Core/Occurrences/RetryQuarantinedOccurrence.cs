// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Re-arms quarantined work, atomically recording the authenticated operator.
/// </summary>
/// <param name="EventStore">The target store.</param>
/// <param name="Namespace">The namespace or null for application scope.</param>
/// <param name="Trigger">The trigger.</param>
/// <param name="OccurrenceId">The occurrence identity.</param>
/// <param name="ApplicationScope">Selects event-store application storage.</param>
[Command]
[Authorize]
[BelongsTo(WellKnownServices.Occurrences)]
public record RetryQuarantinedOccurrence(EventStoreName EventStore, string? Namespace, string Trigger, string OccurrenceId, bool ApplicationScope = false)
{
    /// <summary>
    /// Resolves the actor from the authenticated request, never from caller input.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="principalAccessor">The authenticated principal.</param>
    /// <returns>Whether quarantined work was re-armed.</returns>
    public Task<bool> Handle(IGrainFactory grainFactory, ICurrentPrincipalAccessor principalAccessor)
    {
        var principal = principalAccessor.Current;
        var actor = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal?.FindFirst("sub")?.Value ?? principal?.Identity?.Name ?? "anonymous";
        return OccurrenceQueueKey.Resolve(grainFactory, EventStore, ApplicationScope ? null : Namespace, Trigger).Retry(OccurrenceId, actor);
    }
}
