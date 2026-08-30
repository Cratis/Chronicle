// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ImplementationValues.when_converting_to_domain;

/// <summary>
/// An optional concept parameter is a nullable reference type, not a nullable value type, so it does not hit the
/// refusal above it - it used to fall through to the plain cast, and a concept's implicit conversion does not
/// accept null, so every generated call site warned. The conversion is guarded instead, which is also what keeps
/// "absent on the wire" meaning "absent in the domain" rather than an empty concept.
/// </summary>
public class and_the_type_is_an_optional_concept : Specification
{
    string _result = null!;

    void Because() => _result = ImplementationValues.ToDomain("request.Id", typeof(ProductId), isNullable: true);

    [Fact] void should_guard_the_cast_against_null() =>
        _result.ShouldEqual("(request.Id is null ? null : (global::TestAssembly.Catalog.ProductId)request.Id)");
}
