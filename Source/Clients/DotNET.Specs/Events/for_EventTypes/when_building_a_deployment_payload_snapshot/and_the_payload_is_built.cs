// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_building_a_deployment_payload_snapshot;

public class and_the_payload_is_built : given.a_prepared_registry
{
    Task Because() => Build();

    async Task Build() => _snapshot = await _registry.BuildDeploymentPayloadSnapshot();

    [Fact] void should_preserve_the_live_schema_instance() => _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id).ShouldBeSame(_liveSchema);
    [Fact] void should_build_the_same_wire_schema() => _snapshot.Types.Single().Schema.ShouldEqual(_ordinary.Types.Single().Schema);
    [Fact] void should_not_send_the_payload() => _eventTypesService.DidNotReceiveWithAnyArgs().RegisterEventTypes(default!);
}
