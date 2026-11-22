// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes;

public sealed partial class EventTypeRegistrar
{
    static async Task<(List<EventTypeToRegister> Remaining, HashSet<EventTypeId> Mutated, List<NewGenerations> Added, List<EventTypeSchemaChanged> Conflicts)> RegisterNewGenerations(
        IEventTypesStorage storage,
        List<EventTypeToRegister> registrations,
        List<NewGenerations> newGenerations)
    {
        var remaining = new List<EventTypeToRegister>();
        var mutated = new HashSet<EventTypeId>();
        var added = new List<NewGenerations>();
        var conflicts = new List<EventTypeSchemaChanged>();
        foreach (var (registration, pending) in registrations.Zip(newGenerations))
        {
            var generations = pending.Generations.Select(_ => _.Generation).ToHashSet();
            if (generations.Count > 0)
            {
                var incoming = registration with
                {
                    Definition = registration.Definition with
                    {
                        Generations = registration.Definition.Generations.Where(_ => generations.Contains(_.Generation)).ToList()
                    }
                };
                var result = await storage.RegisterIfAbsent(incoming);
                if (result.Conflict is { } conflict)
                {
                    conflicts.Add(new EventTypeSchemaChanged(registration.Definition.Id, conflict));
                    continue;
                }

                if (result.Changed)
                {
                    mutated.Add(registration.Definition.Id);
                }

                added.Add(pending with
                {
                    EventTypeAlreadyStored = result.EventTypeAlreadyStored,
                    Generations = pending.Generations.Where(_ => result.AddedGenerations.Contains(_.Generation)).ToList()
                });
            }

            remaining.Add(registration with
            {
                Definition = registration.Definition with
                {
                    Generations = registration.Definition.Generations.Where(_ => !generations.Contains(_.Generation)).ToList()
                }
            });
        }

        return (remaining, mutated, added, conflicts);
    }

    static void ThrowConflicts(List<EventTypeSchemaChanged> conflicts)
    {
        if (conflicts.Count == 1)
        {
            throw conflicts[0];
        }

        if (conflicts.Count > 1)
        {
            throw new AggregateException(conflicts);
        }
    }
}
