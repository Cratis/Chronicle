// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Specs.Events.Constraints.for_IUniqueEventTypesConstraintsStorage.given;

/// <summary>
/// A storage provider written against the interface as it shipped: it implements the original member and nothing
/// else, exactly as a third-party assembly compiled before the typed member existed does.
/// <para>
/// Deliberately a real class rather than a substitute. A default interface implementation is only reached through
/// real interface dispatch; a mocking framework intercepts the call and answers without ever running the default
/// body, so a substitute would prove nothing about what such a provider actually receives.
/// </para>
/// </summary>
public class a_provider_implementing_only_the_original_member : IUniqueEventTypesConstraintsStorage
{
    public UniqueEventTypeConstraintDefinition? ReceivedDefinition { get; private set; }

    public EventSourceId? ReceivedEventSourceId { get; private set; }

    public string? ReceivedScopeKey { get; private set; }

    public int TimesCalled { get; private set; }

    public (bool IsAllowed, EventSequenceNumber SequenceNumber) Answer { get; set; } = (true, EventSequenceNumber.Unavailable);

    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, string scopeKey = "")
    {
        ReceivedDefinition = definition;
        ReceivedEventSourceId = eventSourceId;
        ReceivedScopeKey = scopeKey;
        TimesCalled++;
        return Task.FromResult(Answer);
    }
}
