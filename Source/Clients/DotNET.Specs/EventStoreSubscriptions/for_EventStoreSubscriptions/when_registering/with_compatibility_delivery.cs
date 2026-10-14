// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions.when_registering;

public class with_compatibility_delivery : given.subscriptions
{
    byte[] _wire;
    async Task Because()
    {
        await _subscriptions.Subscribe("s", "source", builder => builder.WithEventType<CustomerRegistered>());
        await using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, _request);
        _wire = stream.ToArray();
    }
    [Fact] void should_still_send_generation_one() => _request.EventTypes.Single().Generation.ShouldEqual(1u);
    [Fact] void should_preserve_the_original_wire_bytes() => Convert.ToHexString(_wire).ShouldEqual("0A01731206736F757263651A0C0A08637573746F6D65721001");
    [Fact] void should_default_to_compatibility() => _request.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
}
