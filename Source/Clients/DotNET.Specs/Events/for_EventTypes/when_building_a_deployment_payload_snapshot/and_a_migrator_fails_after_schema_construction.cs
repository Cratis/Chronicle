// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_building_a_deployment_payload_snapshot;

public class and_a_migrator_fails_after_schema_construction : given.a_prepared_registry
{
    Exception _failure;
    Exception _registrationFailure;
    bool _schemaUnchanged;

    async Task Because()
    {
        _eventTypeMigrators.GetMigratorsFor(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("diagnostic migrator failure"));
        _failure = await Catch.Exception(_registry.BuildDeploymentPayloadSnapshot);
        _schemaUnchanged = ReferenceEquals(_registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id), _liveSchema);
        _eventTypeMigrators.GetMigratorsFor(Arg.Any<Type>()).Returns([]);
        _registrationFailure = await Catch.Exception(_registry.Register);
    }

    [Fact] void should_fail_the_snapshot() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_live_schema() => _schemaUnchanged.ShouldBeTrue();
    [Fact] void should_not_cache_the_diagnostic_failure() => _registrationFailure.ShouldBeNull();
}
