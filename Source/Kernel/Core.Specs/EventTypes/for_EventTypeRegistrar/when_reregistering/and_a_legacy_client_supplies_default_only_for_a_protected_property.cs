// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_a_legacy_client_supplies_default_only_for_a_protected_property : given.a_registration
{
    void Establish() => StoredEventTypes(StoredEventType("decimal", (1, Protected)));

    Task Because() => Register(DefaultOnly);

    [Fact] void should_keep_the_protected_schema() => _registered.Definition.Generations.Single().Schema.ToJson().ShouldEqual(Protected);
}
