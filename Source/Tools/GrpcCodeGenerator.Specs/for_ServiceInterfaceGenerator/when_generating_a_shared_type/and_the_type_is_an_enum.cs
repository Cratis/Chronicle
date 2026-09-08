// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_a_shared_type;

/// <summary>
/// An enum needs no protobuf attributes at all - protobuf-net serializes by declared value - so the entire
/// wire-stability story is copying the existing values verbatim. This is what the real <c>JobStatus</c> move
/// exercised; the spec pins the same shape down.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_an_enum : given.a_shared_type_generator
{
    string _code = null!;
    (IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic> Errors, Assembly? Assembly) _compiled;

    void Because()
    {
        _code = _generator.GenerateSharedType(typeof(CoreOwnedStatus), _outputDir);
        _compiled = for_ServiceImplementationGenerator.given.GeneratedSourceCompiler.Compile(_code);
    }

    [Fact] void should_compile_without_errors() => _compiled.Errors.ShouldBeEmpty();
    [Fact] void should_carry_no_proto_contract_attribute() => _code.ShouldNotContain("[ProtoContract]");
    [Fact] void should_carry_no_proto_member_attributes() => _code.ShouldNotContain("[ProtoMember");
    [Fact] void should_preserve_the_first_value() => _code.ShouldContain("First = 0");
    [Fact] void should_preserve_the_second_value() => _code.ShouldContain("Second = 1");
}
