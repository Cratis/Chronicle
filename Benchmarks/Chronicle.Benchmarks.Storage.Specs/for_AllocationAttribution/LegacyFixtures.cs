// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>The unchanged substitute behavior from baseline 500d19c180; never used by the executable.</summary>
internal static class LegacyFixtures
{
    /// <summary>Creates the baseline's recording, per-lookup empty-schema fixture.</summary>
    /// <returns>The baseline fixture.</returns>
    public static IEventTypesStorage EventTypes()
    {
        var types = Substitute.For<IEventTypesStorage>();
        types.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(true);
        types.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(call =>
            new EventTypeSchema(new EventType(call.Arg<EventTypeId>(), EventTypeGeneration.First), EventTypeOwner.None, EventTypeSource.Unknown, new JsonSchema()));
        return types;
    }

    /// <summary>Creates the baseline's recording system-identity fixture.</summary>
    /// <returns>The baseline fixture.</returns>
    public static IIdentityStorage Identities()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        return identities;
    }
}
