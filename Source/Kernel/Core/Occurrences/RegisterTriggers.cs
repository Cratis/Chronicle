// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Registers event-store-level contracts before recording trigger occurrences.
/// </summary>
/// <param name="EventStore">The target event store.</param>
/// <param name="Triggers">The trigger payload contracts.</param>
[Command]
[BelongsTo(WellKnownServices.Occurrences)]
public record RegisterTriggers(EventStoreName EventStore, IEnumerable<TriggerRegistration> Triggers)
{
    /// <summary>
    /// Validates and registers the trigger definitions.
    /// </summary>
    /// <param name="storage">The infrastructure storage.</param>
    /// <param name="registrar">The payload contract registrar.</param>
    /// <returns>The registration outcome.</returns>
    public Task<TriggerRegistrationResult> Handle(IStorage storage, TriggerRegistrar registrar) => registrar.Register(storage.GetEventStore(EventStore).TriggerDefinitions, Triggers);
}
