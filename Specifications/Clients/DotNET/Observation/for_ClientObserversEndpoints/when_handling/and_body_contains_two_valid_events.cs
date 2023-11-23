// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Aksio.Commands;
using Aksio.Cratis.Specifications.Events;
using Aksio.Json;

namespace Aksio.Cratis.Observation.for_ClientObserversEndpoints.when_handling;

public class and_body_contains_two_valid_events : given.an_http_context_and_client_observers
{
    CommandResult result;
    EventSequenceNumber last_event;

    void Establish()
    {
        route_values.Add("observerId", Guid.NewGuid().ToString());
        var first = AppendedEventFactory.Create(new EventSequenceNumber(1), new object());
        var second = AppendedEventFactory.Create(new EventSequenceNumber(2), new object());
        request.SetupGet(_ => _.ContentType).Returns("application/json");
        var payload = JsonSerializer.SerializeToUtf8Bytes(new[] { first, second }, Globals.JsonSerializerOptions);
        request.SetupGet(_ => _.Body).Returns(new MemoryStream(payload));
        client_observers.Setup(_ => _.OnNext(IsAny<ObserverId>(), IsAny<IEnumerable<AppendedEvent>>())).Returns(Task.FromResult(ObserverInvocationResult.Success(new EventSequenceNumber(2))));
    }

    async Task Because()
    {
        await ClientObserversEndpoints.Handler(http_context.Object);
        result = DeserializeResponse<CommandResult>()!;
        last_event = ((JsonElement)result.Response).Deserialize<EventSequenceNumber>(Globals.JsonSerializerOptions);
    }

    [Fact] void should_invoke_client_observers() => client_observers.Verify(_ => _.OnNext(IsAny<ObserverId>(), IsAny<IEnumerable<AppendedEvent>>()), Once);
    [Fact] void should_return_ok() => response.VerifySet(_ => _.StatusCode = (int)HttpStatusCode.OK);
    [Fact] void result_should_be_successful() => result.IsValid.ShouldBeTrue();
    [Fact] void result_should_hold_last_successfully_observed_event() => last_event.ShouldEqual(new EventSequenceNumber(2));
}
