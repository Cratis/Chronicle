// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_a_legacy_client_supplies_default_only : given.a_registration
{
    void Establish() => StoredEventTypes(StoredEventType("decimal", (1, Typed)));

    Task Because() => Register(DefaultOnly);

    [Fact] void should_keep_the_precise_schema() => _registered.Definition.Generations.Single().Schema.ToJson().ShouldEqual(Typed);
}
