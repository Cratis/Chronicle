// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintsByBuilderProvider.when_using_an_explicit_definer;

public class and_the_class_has_no_marker : an_explicit_store
{
    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("event-alias")).RegisterConstraint<PlainDefiner>();
        _activator.ActivateNonDisposable(typeof(PlainDefiner)).Returns(new PlainDefiner());
        _store = CreateStore();
    }

    async Task Because() => await _store.DiscoverAll();

    [Fact] void should_build_a_real_constraint_over_the_explicit_event() => _store.Constraints.HasFor("event-alias").ShouldBeTrue();
    [Fact] void should_preserve_the_existing_constraint_kind() => _store.Constraints.GetFor("event-alias").ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();

    public class PlainDefiner
    {
        public void Define(IConstraintBuilder builder) => builder.Unique<PlainEvent>();
    }
}
