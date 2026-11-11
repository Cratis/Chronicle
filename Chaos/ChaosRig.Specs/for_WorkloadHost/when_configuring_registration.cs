// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_WorkloadHost;

/// <summary>
/// Specifies role-specific registration that preserves consumer artifacts.
/// </summary>
public class when_configuring_registration : Specification
{
    ChronicleClientOptions _producer = new();
    ChronicleClientOptions _consumer = new();
    ChronicleClientOptions _reader = new();

    void Because()
    {
        WorkloadHost.ConfigureOptions(_producer, "producer", "chronicle://localhost:35000", "chaos");
        WorkloadHost.ConfigureOptions(_consumer, "consumer", "chronicle://localhost:35000", "chaos");
        WorkloadHost.ConfigureOptions(_reader, "reader", "chronicle://localhost:35000", "chaos");
    }

    [Fact] void should_register_only_producer_event_types() => _producer.RegistrationMode.ShouldEqual(RegistrationMode.EventTypesOnly);
    [Fact] void should_register_consumer_artifacts() => _consumer.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
    [Fact] void should_enable_producer_registration() => _producer.AutoDiscoverAndRegister.ShouldBeTrue();
    [Fact] void should_disable_reader_registration() => _reader.AutoDiscoverAndRegister.ShouldBeFalse();
}
