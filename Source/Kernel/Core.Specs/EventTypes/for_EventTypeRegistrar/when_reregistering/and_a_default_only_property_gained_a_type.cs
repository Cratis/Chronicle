// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_a_default_only_property_gained_a_type : given.a_registration
{
    void Establish() => StoredEventTypes(StoredEventType("decimal", (1, DefaultOnly)));

    Task Because() => Register(Typed);

    [Fact] void should_upgrade_the_stored_schema() => _registered.Definition.Generations.Single().Schema.ActualProperties["amount"].Format.ShouldEqual("decimal");
}
