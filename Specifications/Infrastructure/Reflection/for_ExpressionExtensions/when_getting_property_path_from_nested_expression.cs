// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Reflection.for_ExpressionExtensions;

public class when_getting_property_path_from_nested_expression : Specification
{
    record ThirdLevel(int SomeInteger);
    record SecondLevel(ThirdLevel Third);
    record TopLevel(SecondLevel Second);

    Expression expression;
    PropertyPath result;
    PropertyPath expected = new($"{nameof(TopLevel.Second)}.{nameof(SecondLevel.Third)}.{nameof(ThirdLevel.SomeInteger)}");

    void Establish() => expression = (TopLevel instance) => instance.Second.Third.SomeInteger;

    void Because() => result = expression.GetPropertyPath();

    [Fact] void should_produce_correct_path() => result.Path.ShouldEqual(expected.Path);
}
