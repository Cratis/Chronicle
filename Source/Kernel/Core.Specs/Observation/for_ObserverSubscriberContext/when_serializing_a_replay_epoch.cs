// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Json;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberContext;

public class when_serializing_a_replay_epoch : Specification
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a"));
    ObserverSubscriberContext _result;
    ObserverSubscriberContext _withoutEpoch;

    void Because()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriberContextJsonConverter, ObserverSubscriberContext>());
        var context = new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "A" }) { ReplayEpoch = _epoch };
        _result = JsonSerializer.Deserialize<ObserverSubscriberContext>(JsonSerializer.Serialize(context, options), options)!;
        var plain = new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "A" });
        _withoutEpoch = JsonSerializer.Deserialize<ObserverSubscriberContext>(JsonSerializer.Serialize(plain, options), options)!;
    }

    [Fact] void should_preserve_the_replay_epoch() => _result.ReplayEpoch.ShouldEqual(_epoch);
    [Fact] void should_preserve_the_absence_of_an_epoch() => _withoutEpoch.ReplayEpoch.ShouldBeNull();
}
