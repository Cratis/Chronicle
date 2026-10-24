// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_initialization_state_is_internal : given.a_composite_key_read
{
    void Establish() => ((IDictionary<string, object?>)_instance)[WellKnownProperties.ReadModelInstanceInitialized] = true;
    Task Because() => Read();
    [Fact] void should_not_serialize_initialization_state() => ((IDictionary<string, object?>)_serialized).ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
    [Fact] void should_leave_the_stored_instance_unchanged() => ((IDictionary<string, object?>)_instance).ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeTrue();
}
