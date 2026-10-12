// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle;

/// <summary>
/// Creates isolated registries without requiring declarations on their CLR types.
/// </summary>
internal static class RegisteredEventTypesForSpecifications
{
    /// <summary>
    /// Creates a registry that rejects unknown types.
    /// </summary>
    /// <param name="registrations">The types and identities known to this registry.</param>
    /// <returns>The registry.</returns>
    internal static IEventTypes Create(params (Type Type, EventType Identity)[] registrations)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(registrations.Select(entry => entry.Type).ToImmutableList());
        registry.All.Returns(registrations.Select(entry => entry.Identity).ToImmutableList());
        registry.HasFor(Arg.Any<Type>()).Returns(call => registrations.Any(entry => entry.Type == call.Arg<Type>()));
        registry.HasFor(Arg.Any<EventTypeId>()).Returns(call => registrations.Any(entry => entry.Identity.Id == call.Arg<EventTypeId>()));
        registry.GetEventTypeFor(Arg.Any<Type>()).Returns(call =>
            registrations.Where(entry => entry.Type == call.Arg<Type>()).Select(entry => entry.Identity).SingleOrDefault()
                ?? throw new TypeIsNotAnEventType(call.Arg<Type>()));
        return registry;
    }
}
