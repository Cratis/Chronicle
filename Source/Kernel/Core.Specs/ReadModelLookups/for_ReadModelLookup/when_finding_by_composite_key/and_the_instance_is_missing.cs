// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_the_instance_is_missing : given.a_composite_key_read
{
    void Establish() => _store.FindByCompositeKey(EventStore, Namespace, _definition, Arg.Any<Key>()).Returns(CompositeKeyLookupResult.NotFound);
    Task Because() => Read();
    [Fact] void should_report_not_found() => _result.Outcome.ShouldEqual(ReadModelLookupOutcome.NotFound);
    [Fact] void should_return_json_null() => _result.ReadModel.ShouldEqual("null");
    [Fact] void should_return_an_unavailable_watermark() => _result.LastHandledEventSequenceNumber.ShouldEqual((ulong)EventSequenceNumber.Unavailable);
    [Fact] void should_not_serialize_anything() => _serialized.ShouldBeNull();
}
