// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistry;

public class when_discovering_duplicate_triggers : given.an_occurrence_client
{
    [Trigger("test-trigger")]
    record Duplicate;

    void Establish()
    {
        _artifacts.Triggers.Returns([typeof(given.TestTrigger), typeof(Duplicate)]);
        _schemas.Generate(typeof(Duplicate)).Returns(new Schemas.JsonSchema());
    }

    void Because() => _error = Catch.Exception(_registry.Discover);

    [Fact] void should_refuse_duplicate_identities() => _error.ShouldBeOfExactType<TriggerNotValid>();
    [Fact] void should_not_publish_a_partial_catalog() => Catch.Exception(() => _registry.Get(typeof(given.TestTrigger))).ShouldBeOfExactType<TriggerNotValid>();
}
