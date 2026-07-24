// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class a_global_event_seeding_grain : an_event_seeding_grain
{
    void Establish()
    {
        _key = EventSeedingKey.ForGlobal("TestEventStore");

        var keyField = typeof(EventSeeding).GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        keyField.SetValue(_grain, _key);
    }
}
