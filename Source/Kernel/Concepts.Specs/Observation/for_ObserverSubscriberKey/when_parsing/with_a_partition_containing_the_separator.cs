// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberKey.when_parsing;

public class with_a_partition_containing_the_separator : Specification
{
    const string Partition = "repository#1563531199";
    const string SiloAddress = "127.0.0.1:11111@1";
    ObserverSubscriberKey _result;

    void Because() => _result = ObserverSubscriberKey.Parse(
        new ObserverSubscriberKey("observer", "store", "namespace", "sequence", Partition, SiloAddress).ToString());

    [Fact] void should_preserve_the_partition() => _result.EventSourceId.Value.ShouldEqual(Partition);
    [Fact] void should_preserve_the_silo_address() => _result.SiloAddress.ShouldEqual(SiloAddress);
}
