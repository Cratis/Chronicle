// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_compliance_is_removed_with_validation_disabled : given.a_registration
{
    Exception _exception;

    void Establish() => StoredEventTypes(StoredEventType("decimal", (1, Protected)));

    async Task Because() => _exception = await Catch.Exception(() => Register(Typed, true));

    [Fact] void should_refuse_the_removal() => _exception.ShouldBeOfExactType<EventTypeSchemaChanged>();
}
