// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Google.Protobuf.Reflection;
using ProtoBuf;
using Wire = Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_serializing_for_the_wire;

public class and_the_distance_is_zero_or_absent : Specification
{
    Wire.ObserverLagResponse _measuredZero;
    Wire.ObserverLagResponse _absent;
    DescriptorProto _response;
    DescriptorProto _wrapper;
    string _wrapperTypeName;

    void Establish()
    {
        using var descriptor = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "chronicle.desc"));
        var schema = Serializer.Deserialize<FileDescriptorSet>(descriptor).Files.Single(file => file.Name == "observation.proto");
        _response = schema.MessageTypes.Single(message => message.Name == nameof(Wire.ObserverLagResponse));
        _wrapper = schema.MessageTypes.Single(message => message.Name == "OptionalUInt64");
        _wrapperTypeName = $".{schema.Package}.{_wrapper.Name}";
    }

    void Because()
    {
        _measuredZero = RoundTrip(new Wire.ObserverLagResponse { Status = Wire.ObserverLagStatus.CaughtUp, Distance = 0UL, NextEventSequenceNumber = 0UL });
        _absent = RoundTrip(new Wire.ObserverLagResponse { Status = Wire.ObserverLagStatus.Unmeasurable });
    }

    [Fact] void should_keep_a_measured_zero_distance() => (_measuredZero.Distance?.Value).ShouldEqual((ulong?)0);
    [Fact] void should_keep_a_measured_zero_position() => (_measuredZero.NextEventSequenceNumber?.Value).ShouldEqual((ulong?)0);
    [Fact] void should_keep_an_absent_distance_absent() => _absent.Distance.ShouldBeNull();
    [Fact] void should_keep_an_absent_position_absent() => _absent.NextEventSequenceNumber.ShouldBeNull();

    [Fact]
    void should_export_all_positions_and_distance_as_presence_carrying_messages() => _response.Fields
        .Where(field => field.type == FieldDescriptorProto.Type.TypeMessage && field.TypeName == _wrapperTypeName)
        .Select(field => field.Number).Order().ShouldEqual([7, 8, 9, 10, 11]);

    [Fact]
    void should_export_the_wrapper_value_as_an_unsigned_number() => _wrapper.Fields
        .Select(field => (field.Number, field.type)).ShouldEqual([(1, FieldDescriptorProto.Type.TypeUint64)]);

    static Wire.ObserverLagResponse RoundTrip(Wire.ObserverLagResponse response)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, response);
        stream.Position = 0;
        return Serializer.Deserialize<Wire.ObserverLagResponse>(stream);
    }
}
