// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_reading_the_released_list_format : given.a_normalized_event
{
    List<AppendedEvent> _result;

    void Because() => _result = service_provider.GetRequiredService<Serializer<List<AppendedEvent>>>().Deserialize(Convert.FromBase64String(given.LegacyWireFixtures.List))!;

    [Fact] void should_read_three_events() => _result.Count.ShouldEqual(3);

    [Fact] void should_preserve_the_envelopes_and_content_in_order() => JsonSerializer.Serialize(_result, options).ShouldEqual(JsonSerializer.Serialize(event_batch, options));

    [Fact] void should_preserve_nested_runtime_types() => _result.SelectMany(item => ContentTypes(item.Content)).ShouldEqual(event_batch.SelectMany(item => ContentTypes(item.Content)));
}
