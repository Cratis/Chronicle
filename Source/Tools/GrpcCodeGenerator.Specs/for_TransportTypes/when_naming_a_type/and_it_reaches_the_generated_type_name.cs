// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_TransportTypes.when_naming_a_type;

/// <summary>
/// The substitution is only useful if it reaches what the generator actually writes, including through the wrappers
/// an artifact property can arrive in.
/// </summary>
public class and_it_reaches_the_generated_type_name : Specification
{
    const string SerializableDateTimeOffset = $"global::{TransportTypes.PrimitivesNamespace}.{nameof(Contracts.Primitives.SerializableDateTimeOffset)}";

    [Fact]
    void should_substitute_a_bare_property() =>
        TypeHelper.GetTypeName(typeof(DateTimeOffset)).ShouldEqual(SerializableDateTimeOffset);

    [Fact]
    void should_substitute_a_nullable_property() =>
        TypeHelper.GetTypeName(typeof(DateTimeOffset?)).ShouldEqual(SerializableDateTimeOffset);

    [Fact]
    void should_substitute_inside_a_collection() =>
        TypeHelper.GetTypeName(typeof(IEnumerable<DateTimeOffset>)).ShouldEqual($"IEnumerable<{SerializableDateTimeOffset}>");

    [Fact]
    void should_leave_types_protobuf_handles_alone() =>
        TypeHelper.GetTypeName(typeof(Guid)).ShouldEqual("Guid");
}
