// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Reflection.for_ExpressionExtensions;

public class when_getting_property_path_from_expression : Specification
{
    record TheType(int SomeInteger);

    Expression expression;
    PropertyPath result;
    PropertyPath expected = new(nameof(TheType.SomeInteger));

    void Establish() => expression = (TheType instance) => instance.SomeInteger;

    void Because() => result = expression.GetPropertyPath();

    [Fact] void should_produce_correct_path() => result.Path.ShouldEqual(expected.Path);
}
