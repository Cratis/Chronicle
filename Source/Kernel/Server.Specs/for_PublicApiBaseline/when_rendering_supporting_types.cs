// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Supporting CLR types, not only service methods and DTO properties, are protected.
/// </summary>
public class when_rendering_supporting_types : Specification
{
    string _snapshot = string.Empty;
    string _nestedName = string.Empty;
    string _matrixName = string.Empty;

    void Because()
    {
        _snapshot = given.public_api_snapshot.Render([typeof(given.a_sample_shapes<>), typeof(given.a_sample_flags)]);
        _nestedName = given.public_api_type_name.Render(typeof(given.a_sample_shapes<>.a_nested_shape<>));
        var nullability = new System.Reflection.NullabilityInfoContext().Create(typeof(given.a_sample_shapes<>).GetProperty(nameof(given.a_sample_shapes<string>.Items))!);
        _matrixName = given.public_api_type_name.Render(typeof(string).MakeArrayType(2), nullability);
    }

    [Fact] void should_include_constants_without_physical_line_breaks() => _snapshot.ShouldContain("const System.String! = \"first\\nsecond\"");
    [Fact] void should_include_readonly_fields() => _snapshot.ShouldContain("public static readonly System.Int32");
    [Fact] void should_include_required_semantics() => _snapshot.ShouldContain("System.Runtime.CompilerServices.RequiredMemberAttribute");
    [Fact] void should_include_enum_underlying_types() => _snapshot.ShouldContain("base=System.UInt64");
    [Fact] void should_include_unsigned_enum_values() => _snapshot.ShouldContain(" = 9223372036854775808");
    [Fact] void should_include_flags_semantics() => _snapshot.ShouldContain("System.FlagsAttribute");
    [Fact] void should_include_events() => _snapshot.ShouldContain("::Changed public System.Action");
    [Fact] void should_include_event_accessors() => _snapshot.ShouldContain("::Changed add public calling=");
    [Fact] void should_include_array_rank_and_nullability() => _matrixName.ShouldEqual("System.String?[,]?");
    [Fact] void should_disambiguate_nested_generic_parameters() => _nestedName.ShouldContain("a_sample_shapes<!0:T>+a_nested_shape<!1:TItem>");
}
