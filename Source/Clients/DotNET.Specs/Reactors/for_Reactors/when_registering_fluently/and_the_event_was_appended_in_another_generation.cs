// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_fluently;

public class and_the_event_was_appended_in_another_generation : given.an_observed_reactor_stream
{
    async Task Because()
    {
        await _reactors.Register("orders", reactor => reactor.On<OrderPlaced>(_ => { }));
        Deliver(_orderPlacedV1, "{\"number\":\"42\"}", new() { [1] = "{\"number\":\"42\"}", [2] = "{\"orderNumber\":\"42\"}" });
        await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_deserialize_the_subscribed_generations_content() => _deserialized.Single().ContainsKey("orderNumber").ShouldBeTrue();
}
