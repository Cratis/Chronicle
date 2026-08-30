// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_TransportTypes.when_naming_a_type;

/// <summary>
/// protobuf-net emits an opaque, empty message for a type it cannot represent - a schema that parses, generates and
/// compiles while transmitting nothing. That shipped: ten fields across four packages carried no value to any
/// non-.NET client, and only a cross-language read would ever have shown it.
/// </summary>
/// <remarks>
/// So the rule these specifications hold is not "map DateTimeOffset" - it is that a type protobuf cannot represent
/// never passes silently. It is substituted, or generation stops.
/// </remarks>
public class and_protobuf_cannot_represent_it : Specification
{
    /// <summary>
    /// The stand-in is written into a file generated one service at a time into one namespace each, and the
    /// primitives live in another - so the name it is referred to by has to carry its own namespace.
    /// </summary>
    const string SerializableDateTimeOffset = $"global::{TransportTypes.PrimitivesNamespace}.{nameof(Contracts.Primitives.SerializableDateTimeOffset)}";

    [Fact]
    void should_stand_in_for_a_date_time_offset() =>
        TransportTypes.NameFor(typeof(DateTimeOffset)).ShouldEqual(SerializableDateTimeOffset);

    [Fact]
    void should_qualify_the_stand_in_with_its_namespace() =>
        TransportTypes.NameFor(typeof(DateTimeOffset))!.StartsWith($"global::{TransportTypes.PrimitivesNamespace}.", StringComparison.Ordinal).ShouldBeTrue();

    [Fact]
    void should_refuse_a_type_it_has_no_stand_in_for() =>
        Catch.Exception(() => TransportTypes.NameFor(typeof(DateOnly))).ShouldBeOfExactType<UnrepresentableTransportType>();

    [Fact]
    void should_name_what_to_do_about_it() =>
        Catch.Exception(() => TransportTypes.NameFor(typeof(TimeOnly)))
            .Message.ShouldContain("Source/Kernel/Contracts/Primitives");

    [Fact]
    void should_leave_a_type_protobuf_handles_alone() =>
        TransportTypes.NameFor(typeof(Guid)).ShouldBeNull();

    [Fact]
    void should_leave_a_scalar_alone() =>
        TransportTypes.NameFor(typeof(int)).ShouldBeNull();
}
