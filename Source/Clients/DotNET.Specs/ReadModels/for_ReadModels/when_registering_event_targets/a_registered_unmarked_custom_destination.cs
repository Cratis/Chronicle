// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_registered_unmarked_custom_destination : given.a_registered_unmarked_target<a_registered_unmarked_custom_destination.Target>
{
    async Task Because() => await RegisterBoth();

    [Fact] void should_register_both_paths() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_use_the_custom_destination() => _definitions.TrueForAll(definition => definition.Sink.EventSequence.EventSequence == "partner-feed").ShouldBeTrue();
    [Fact] void should_keep_publication_private() => _definitions.TrueForAll(definition => !definition.Sink.EventSequence.IsPublic).ShouldBeTrue();
    [Fact] void should_not_fail_bulk_registration() => _bulkError.ShouldBeNull();
    [Fact] void should_not_fail_generic_registration() => _genericError.ShouldBeNull();

    [PublishTo("partner-feed")]
    public record Target(string Name);
}
