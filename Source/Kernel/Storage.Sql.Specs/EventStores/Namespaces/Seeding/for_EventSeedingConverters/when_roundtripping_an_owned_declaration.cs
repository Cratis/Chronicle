// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Seeding.for_EventSeedingConverters;

public class when_roundtripping_an_owned_declaration : Specification
{
    EventSeeds _seeds;
    EventSeeds _result;

    void Establish()
    {
        var entry = new SeededEventEntry("source", "type", "{}", []) { Origin = SeedOrigin.Declared, Owner = "program" };
        _seeds = new(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>> { [entry.EventTypeId] = [entry] },
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>> { [entry.EventSourceId] = [entry] });
    }

    void Because() => _result = EventSeedingConverters.ToEventSeeds(EventSeedingConverters.ToEntity(_seeds, JsonSerializerOptions.Default), JsonSerializerOptions.Default);

    [Fact] void should_preserve_origin() => _result.ByEventType.Values.Single().Single().Origin.ShouldEqual(SeedOrigin.Declared);
    [Fact] void should_preserve_owner() => _result.ByEventSource.Values.Single().Single().Owner.ShouldEqual("program");
}
