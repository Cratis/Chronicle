// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_manual_mode_has_not_discovered_options : given.an_explicit_store
{
    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _store = CreateStore();
    }

    async Task Because() => await _store.RegisterAll();

    [Fact] void should_keep_manual_event_membership_untouched() => _store.EventTypes.All.ShouldBeEmpty();
    [Fact] void should_register_the_valid_empty_manual_registry() => _payload.Types.ShouldBeEmpty();
    [Fact] void should_not_apply_absent_options_recipes() => _store.EventTypes.HasFor(typeof(PlainEvent)).ShouldBeFalse();
    [Fact] void should_report_the_manual_registration_outcome() => _store.Registration.IsSuccess.ShouldBeTrue();
}
