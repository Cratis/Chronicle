// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// The offline renderer preserves declarations rather than only wire message shapes.
/// </summary>
public class when_rendering_public_contracts : Specification
{
    string _snapshot = string.Empty;
    string _reordered = string.Empty;

    void Because()
    {
        _snapshot = given.public_api_snapshot.Render([typeof(given.ISampleContract), typeof(given.a_sample_message), given.a_sample_wire_message.Contract]);
        _reordered = given.public_api_snapshot.Render([given.a_sample_wire_message.Contract, typeof(given.a_sample_message), typeof(given.ISampleContract)]);
    }

    [Fact] void should_include_service_and_operation_identity() => _snapshot.ShouldContain("ProtoBuf.Grpc.Configuration.ServiceAttribute");
    [Fact] void should_include_operation_identity() => _snapshot.ShouldContain("ProtoBuf.Grpc.Configuration.OperationAttribute");
    [Fact] void should_include_request_and_response_members() => _snapshot.ShouldContain("ISampleContract::Read(");
    [Fact] void should_include_proto_member_indexes() => _snapshot.ShouldContain("ProtoBuf.ProtoMemberAttribute(System.Int32:3)");
    [Fact] void should_include_explicit_wire_options() => _snapshot.ShouldContain("IsRequired=System.Boolean:false");
    [Fact] void should_preserve_optional_defaults() => _snapshot.ShouldContain("System.Int32 limit optional=7");
    [Fact] void should_preserve_nullable_generic_arguments() => _snapshot.ShouldContain("System.Collections.Generic.List<System.String?>?");
    [Fact] void should_preserve_nullable_parameters() => _snapshot.ShouldContain("System.String? filter optional=null");
    [Fact] void should_preserve_init_accessors() => _snapshot.ShouldContain("a_sample_message::Names init public calling=");
    [Fact] void should_exclude_private_setters() => _snapshot.ShouldNotContain("a_sample_message::Value set");
    [Fact] void should_not_repeat_property_signatures_in_accessor_keys() => _snapshot.ShouldNotContain("accessor property");
    [Fact] void should_preserve_nonnullable_properties() => _snapshot.ShouldContain("::Value() public System.String!");
    [Fact] void should_preserve_unknown_nullability() => _snapshot.ShouldContain("ApiBaselineSamples.Message::Names() public System.String~");
    [Fact] void should_ignore_compiler_nullability_attributes() => _snapshot.ShouldNotContain("System.Runtime.CompilerServices.Nullable");
    [Fact] void should_preserve_indexers() => _snapshot.ShouldContain("::Item(System.Int32 index)");
    [Fact] void should_preserve_generic_constraints() => _snapshot.ShouldContain("ReferenceTypeConstraint, DefaultConstructorConstraint");
    [Fact] void should_preserve_in_parameters() => _snapshot.ShouldContain("in System.Int32& count");
    [Fact] void should_preserve_out_parameters() => _snapshot.ShouldContain("out System.String!& value");
    [Fact] void should_preserve_variadic_parameters() => _snapshot.ShouldContain("params !!0:T![]! items");
    [Fact] void should_preserve_record_operators() => _snapshot.ShouldContain("::op_Equality(");
    [Fact] void should_preserve_record_generated_methods() => _snapshot.ShouldContain("::ToString(");
    [Fact] void should_be_independent_of_input_order() => _snapshot.ShouldEqual(_reordered);
}
