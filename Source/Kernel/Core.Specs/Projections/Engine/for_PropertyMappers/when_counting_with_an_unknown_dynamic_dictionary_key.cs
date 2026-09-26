// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_counting_with_an_unknown_dynamic_dictionary_key : Expressions.given.an_appended_event
{
    Exception _result;

    void Because()
    {
        var mapper = PropertyMappers.Count(new TypeFormats(), "counts.$bogus.x", new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int64" });
        _result = Catch.Exception(() => mapper(@event, new ExpandoObject(), ArrayIndexers.NoIndexers));
    }

    [Fact] void should_reject_the_unknown_expression() => _result.ShouldBeOfExactType<UnsupportedDynamicPropertyPathExpression>();
}
