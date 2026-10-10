// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class with_default_delivery_sends_compatibility : given.a_registered_delegate
{
    void Because() { }
    [Fact] void should_send_compatibility_delivery() => _definition.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_default_the_client_options_to_compatibility() => new ChronicleOptions().EventGenerationDelivery.ShouldEqual(Observation.EventGenerationDelivery.Compatibility);
}
