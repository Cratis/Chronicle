// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_initialization_state_is_declared : given.a_composite_key_read
{
    void Establish()
    {
        Schema("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"int64"}}},"__initialized":{"type":"boolean"}}}""");
        ((IDictionary<string, object?>)_instance)[WellKnownProperties.ReadModelInstanceInitialized] = true;
    }
    Task Because() => Read();
    [Fact] void should_keep_declared_initialization_state() => ((IDictionary<string, object?>)_serialized).ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeTrue();
}
