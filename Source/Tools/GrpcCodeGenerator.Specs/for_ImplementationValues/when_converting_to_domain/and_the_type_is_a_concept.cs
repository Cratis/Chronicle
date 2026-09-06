// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ImplementationValues.when_converting_to_domain;

/// <summary>
/// A concept parameter travels on the wire as its unwrapped primitive - converting it back to the concept is a
/// plain cast, since ConceptAs&lt;T&gt; provides the implicit conversion the other way already.
/// </summary>
public class and_the_type_is_a_concept : Specification
{
    string _result = null!;

    void Because() => _result = ImplementationValues.ToDomain("request.Id", typeof(ProductId));

    [Fact] void should_cast_to_the_concept() =>
        _result.ShouldEqual("(global::TestAssembly.Catalog.ProductId)request.Id");
}
