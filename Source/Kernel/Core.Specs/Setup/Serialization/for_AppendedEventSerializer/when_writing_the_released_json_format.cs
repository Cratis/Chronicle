// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_writing_the_released_json_format : given.a_normalized_event
{
    byte[] _result;
    byte[] _list;
    byte[] _array;

    void Because()
    {
        _result = serializer.SerializeToArray(appended_event);
        _list = service_provider.GetRequiredService<Serializer<List<AppendedEvent>>>().SerializeToArray(event_batch.ToList());
        _array = service_provider.GetRequiredService<Serializer<AppendedEvent[]>>().SerializeToArray(event_batch);
    }

    [Fact] void should_be_byte_identical_to_the_v19_115_0_writer() => _result.ShouldEqual(released_format);

    [Fact] void should_match_the_single_event_golden() => _result.ShouldEqual(Convert.FromBase64String(given.LegacyWireFixtures.Single));

    [Fact] void should_match_the_list_golden() => _list.ShouldEqual(Convert.FromBase64String(given.LegacyWireFixtures.List));

    [Fact] void should_match_the_array_golden() => _array.ShouldEqual(Convert.FromBase64String(given.LegacyWireFixtures.Array));
}
