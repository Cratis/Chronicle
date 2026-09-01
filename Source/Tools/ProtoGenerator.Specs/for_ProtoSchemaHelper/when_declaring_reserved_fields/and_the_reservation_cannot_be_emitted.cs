// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_declaring_reserved_fields.stand_ins;

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_declaring_reserved_fields;

/// <summary>
/// A type that asks for reserved field numbers and does not get them is the one case that must never pass quietly.
/// The generated schema is regenerated wholesale, so a reservation that did not happen leaves the retired number
/// free for reuse again - and the collision only shows up later, on the wire, against data already written.
/// </summary>
/// <remarks>
/// An attribute nothing can be read from is wrong in every package, so it fails on the spot. A missing message is
/// not: one schema is generated per package and every contract type is offered to each of them, so the answer here
/// is "not this one" rather than "not anywhere". What was reserved is reported instead, for the caller to add up
/// across the run - refusing here failed 22 of 23 packages outright and left their .proto files stale.
/// </remarks>
public class and_the_reservation_cannot_be_emitted : Specification
{
    const string SchemaWithoutTheMessage = """
        syntax = "proto3";

        message SomethingElse {
           string name = 1;
        }
        """;

    [Fact]
    void should_refuse_when_the_attribute_carries_no_numbers() =>
        Catch.Exception(() => ProtoSchemaHelper.DeclareReservedFields("message TypeReservingNothing {\n}", [typeof(TypeReservingNothing)]))
            .ShouldBeOfExactType<InvalidOperationException>();

    [Fact]
    void should_leave_a_schema_without_the_message_untouched() =>
        ProtoSchemaHelper.DeclareReservedFields(SchemaWithoutTheMessage, [typeof(TypeWithRetiredFields)]).Schema.ShouldEqual(SchemaWithoutTheMessage);

    [Fact]
    void should_report_nothing_reserved_when_the_schema_has_no_matching_message() =>
        ProtoSchemaHelper.DeclareReservedFields(SchemaWithoutTheMessage, [typeof(TypeWithRetiredFields)]).Declared.ShouldBeEmpty();

    [Fact]
    void should_still_name_the_type_as_needing_a_reservation() =>
        ProtoSchemaHelper.TypesWithRetiredFields([typeof(TypeWithRetiredFields)]).ShouldContainOnly([typeof(TypeWithRetiredFields)]);

    [Fact]
    void should_leave_a_type_that_asked_for_nothing_alone() =>
        ProtoSchemaHelper.DeclareReservedFields(SchemaWithoutTheMessage, [typeof(TypeWithNoAttribute)]).Schema.ShouldEqual(SchemaWithoutTheMessage);

    [Fact]
    void should_not_name_a_type_that_asked_for_nothing_as_needing_a_reservation() =>
        ProtoSchemaHelper.TypesWithRetiredFields([typeof(TypeWithNoAttribute)]).ShouldBeEmpty();
}
