// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_provenance_is_recorded : given.a_backfill
{
    async Task Because() => await Perform();

    [Fact] void should_record_the_applied_definition_version() => _additions[0].Provenance.MigrationsVersion.ShouldEqual(EventTypeMigrationsVersion.For(_definition.Migrations));
    [Fact] async Task should_record_the_version_history() => await _eventTypes.Received(1).RecordMigrationsVersion(TypeId, EventTypeMigrationsVersion.For(_definition.Migrations), _definition.Migrations);
}
