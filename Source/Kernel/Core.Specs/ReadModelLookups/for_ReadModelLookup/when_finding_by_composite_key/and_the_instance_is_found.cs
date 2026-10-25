// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_the_instance_is_found : given.a_composite_key_read
{
    void Establish() => ((IDictionary<string, object?>)_instance)[WellKnownProperties.LastHandledEventSequenceNumber] = 42UL;
    Task Because() => Read();
    [Fact] void should_find_the_instance() => _result.Outcome.ShouldEqual(ReadModelLookupOutcome.Found);
    [Fact] void should_serialize_the_released_instance() => _result.ReadModel.ShouldEqual("{\"name\":\"released\"}");
    [Fact] void should_return_the_watermark() => _result.LastHandledEventSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_normalize_the_parts() => ((IDictionary<string, object?>)(ExpandoObject)_key.Value)["second"].ShouldEqual(42L);
}
