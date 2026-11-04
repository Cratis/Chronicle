// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_a_stale_subset_is_unchanged : given.a_stored_chain
{
    Task Because() => Register();

    [Fact] async Task should_keep_the_live_chain() => EventTypeMigrationsVersion.For((await _memory.GetDefinition(_chain.Id)).Migrations).ShouldEqual(_version);
    [Fact] async Task should_not_record_another_version() => await _eventTypesStorage.DidNotReceive().RecordMigrationsVersion(Arg.Any<EventTypeId>(), Arg.Any<EventTypeMigrationsVersion>(), Arg.Any<IEnumerable<EventTypeMigrationDefinition>>());
    [Fact] async Task should_not_invalidate_peers() => await _eventTypesCacheClient.DidNotReceive().Invalidate(Arg.Any<EventStoreName>(), Arg.Any<EventTypeId>());
    [Fact] async Task should_not_resubscribe_patterns() => await _patternCapture.DidNotReceive().SubscribeAcrossNamespaces(Arg.Any<EventStoreName>());
}
