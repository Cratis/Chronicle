// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Session;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_reading_repeated_legacy_references : given.a_normalized_event
{
    AppendedEvent _first;
    AppendedEvent _second;

    void Because()
    {
        var sessions = service_provider.GetRequiredService<SerializerSessionPool>();
        using var extractSession = sessions.GetSession();
        var extract = Reader.Create(released_format, extractSession);
        var json = StringCodec.ReadValue(ref extract, extract.ReadFieldHeader());
        var buffer = new ArrayBufferWriter<byte>();
        using var writeSession = sessions.GetSession();
        var writer = Writer.Create(buffer, writeSession);
        StringCodec.WriteField(ref writer, 0, json);
        StringCodec.WriteField(ref writer, 0, json);
        writer.Commit();
        using var readSession = sessions.GetSession();
        var reader = Reader.Create(buffer.WrittenSpan, readSession);
        var codec = service_provider.GetServices<IGeneralizedCodec>().OfType<AppendedEventSerializer>().Single();
        _first = (AppendedEvent)codec.ReadValue(ref reader, reader.ReadFieldHeader());
        _second = (AppendedEvent)codec.ReadValue(ref reader, reader.ReadFieldHeader());
    }

    [Fact] void should_read_the_first_legacy_value() => JsonSerializer.Serialize(_first.Context, options).ShouldEqual(JsonSerializer.Serialize(appended_event.Context, options));
    [Fact] void should_read_the_legacy_string_reference() => JsonSerializer.Serialize(_second.Context, options).ShouldEqual(JsonSerializer.Serialize(appended_event.Context, options));
}
