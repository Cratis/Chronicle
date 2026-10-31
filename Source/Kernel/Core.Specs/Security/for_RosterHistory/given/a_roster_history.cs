// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Events.EventSequences;
using Microsoft.AspNetCore.DataProtection;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_RosterHistory.given;

public class a_roster_history : Specification
{
    protected IKernelEventProof _proof;
    protected IKernelEventProof _otherProof;
    internal RosterHistory _history;
    protected UserId _first;
    protected UserId _second;
    protected UserId _third;
    protected readonly List<AppendedEvent> _events = [];
    protected readonly Dictionary<AppendedEvent, object> _contents = [];

    void Establish()
    {
        _proof = new KernelEventProof(new EphemeralDataProtectionProvider());
        _otherProof = new KernelEventProof(new EphemeralDataProtectionProvider());
        _history = new RosterHistory(_proof);
        _first = Guid.NewGuid();
        _second = Guid.NewGuid();
        _third = Guid.NewGuid();
    }

    protected AppendedEvent Add(object content, IKernelEventProof? signer = null, bool omitProof = false)
    {
        var source = content is AdministratorPrivilegeMigrated ? Roster.MigrationEventSourceId : Roster.EventSourceId;
        var predecessor = _events.LastOrDefault(@event => @event.Context.EventSourceId == source)?.Context.SequenceNumber ?? EventSequenceNumber.BeforeFirst;
        if (!omitProof) content = WithProof(content, source, predecessor, signer ?? _proof);
        var appended = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = content.GetType().GetEventType(), EventSourceId = source, SequenceNumber = (ulong)_events.Count }
        };
        _events.Add(appended);
        _contents.Add(appended, content);
        return appended;
    }

    protected static object WithProof(object content, EventSourceId source, EventSequenceNumber predecessor, IKernelEventProof proof)
    {
        var body = content switch
        {
            UserGrantedAdministrator grant => JsonSerializer.Serialize(new[] { grant.UserId.ToString(), ((int)grant.Reason).ToString(CultureInfo.InvariantCulture), grant.Evidence }),
            UserRevokedAdministrator revoke => JsonSerializer.Serialize(new[] { revoke.UserId.ToString(), revoke.RevokeReason }),
            AdministratorPrivilegeMigrated marker => JsonSerializer.Serialize(new[] { marker.Outcome, marker.UserId?.ToString() ?? "" }),
            _ => throw new InvalidOperationException()
        };
        var signed = proof.Sign(content.GetType().GetEventType(), source, predecessor, body);
        return content switch
        {
            UserGrantedAdministrator grant => grant with { Proof = signed },
            UserRevokedAdministrator revoke => revoke with { Proof = signed },
            AdministratorPrivilegeMigrated marker => marker with { Proof = signed },
            _ => throw new InvalidOperationException()
        };
    }

    protected void Redact(AppendedEvent slot)
    {
        var tombstone = slot with { Context = slot.Context with { EventType = new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First) } };
        _events[_events.IndexOf(slot)] = tombstone;
        _contents[tombstone] = new EventRedacted("spec", slot.Context.EventType.Id, DateTimeOffset.UtcNow, slot.Context.CorrelationId, [], []);
    }

    protected void Revise(AppendedEvent slot)
    {
        var revised = slot with { RevisedGeneration = EventTypeGeneration.First };
        _events[_events.IndexOf(slot)] = revised;
        _contents[revised] = _contents[slot];
    }

    protected void Replay()
    {
        foreach (var @event in _events) _history.Apply(@event, appended => _contents[appended]);
    }
}
