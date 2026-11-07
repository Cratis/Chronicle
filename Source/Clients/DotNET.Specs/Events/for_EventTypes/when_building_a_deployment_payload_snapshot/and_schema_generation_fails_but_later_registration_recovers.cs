// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypes.when_building_a_deployment_payload_snapshot;

public class and_schema_generation_fails_but_later_registration_recovers : given.a_prepared_registry
{
    Exception _diagnosticFailure;
    Exception _registrationFailure;
    bool _schemaUnchanged;

    async Task Because()
    {
        _fail = true;
        _diagnosticFailure = await Catch.Exception(_registry.BuildDeploymentPayloadSnapshot);
        _schemaUnchanged = ReferenceEquals(_registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id), _liveSchema);
        _fail = false;
        _registrationFailure = await Catch.Exception(_registry.Register);
    }

    [Fact] void should_report_the_diagnostic_failure() => _diagnosticFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_live_schema() => _schemaUnchanged.ShouldBeTrue();
    [Fact] void should_allow_registration_without_discovery() => _registrationFailure.ShouldBeNull();
    [Fact] void should_send_the_ordinary_payload() => _eventTypesService.Received(1).RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>());
}
