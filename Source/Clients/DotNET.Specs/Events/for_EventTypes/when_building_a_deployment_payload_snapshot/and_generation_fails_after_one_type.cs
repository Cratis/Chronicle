// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_building_a_deployment_payload_snapshot;

public class and_generation_fails_after_one_type : given.a_prepared_registry
{
    JsonSchema _secondSchema;
    int _generated;
    Exception _failure;
    Exception _registrationFailure;
    bool _unchanged;

    async Task Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(AnEvent), typeof(AnotherEvent)]);
        await _registry.Discover();
        await _registry.PrepareRegistration();
        _liveSchema = _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id);
        _secondSchema = _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnotherEvent)).Id);
    }

    async Task Because()
    {
        _schemaGenerator.Generate(Arg.Any<Type>(), Arg.Any<IEventTypes>()).Returns(_ => ++_generated == 2 ? throw new InvalidOperationException("second schema") : new JsonSchema());
        _failure = await Catch.Exception(_registry.BuildDeploymentPayloadSnapshot);
        _unchanged = ReferenceEquals(_liveSchema, _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id)) && ReferenceEquals(_secondSchema, _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnotherEvent)).Id));
        _schemaGenerator.Generate(Arg.Any<Type>(), Arg.Any<IEventTypes>()).Returns(_ => new JsonSchema());
        _registrationFailure = await Catch.Exception(_registry.Register);
    }

    [Fact] void should_generate_one_schema_before_failing() => _generated.ShouldEqual(2);
    [Fact] void should_fail_only_the_snapshot() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_both_live_schemas() => _unchanged.ShouldBeTrue();
    [Fact] void should_not_poison_ordinary_registration() => _registrationFailure.ShouldBeNull();

    [EventType("ecd715e4-0b5b-44ec-9364-dab731fe9bd7")]
    public record AnotherEvent(string Value);
}
