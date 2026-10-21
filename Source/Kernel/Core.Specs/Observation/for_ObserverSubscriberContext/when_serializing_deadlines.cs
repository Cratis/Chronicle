// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Json;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberContext;

public class when_serializing_deadlines : Specification
{
    ObserverSubscriberContext _result;
    readonly DateTimeOffset _deliverBy = DateTimeOffset.UnixEpoch.AddSeconds(30);
    readonly DateTimeOffset _abandonAt = DateTimeOffset.UnixEpoch.AddSeconds(330);

    void Because()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriberContextJsonConverter, ObserverSubscriberContext>());
        var context = new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "A" }) { DeliverBy = _deliverBy, AbandonAt = _abandonAt };
        _result = JsonSerializer.Deserialize<ObserverSubscriberContext>(JsonSerializer.Serialize(context, options), options)!;
    }

    [Fact] void should_preserve_the_delivery_deadline() => _result.DeliverBy.ShouldEqual(_deliverBy);
    [Fact] void should_preserve_the_hold_deadline() => _result.AbandonAt.ShouldEqual(_abandonAt);
    [Fact] void should_preserve_the_metadata_type() => _result.Metadata.ShouldBeOfExactType<ConnectedClient>();
}
