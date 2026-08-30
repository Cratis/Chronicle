// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ImplementationValues.when_converting_to_domain;

/// <summary>
/// A plain primitive already is the domain value - no conversion, the wire expression passes through untouched.
/// </summary>
public class and_the_type_needs_no_conversion : Specification
{
    string _result = null!;

    void Because() => _result = ImplementationValues.ToDomain("request.Name", typeof(string));

    [Fact] void should_pass_the_expression_through_unchanged() => _result.ShouldEqual("request.Name");
}
