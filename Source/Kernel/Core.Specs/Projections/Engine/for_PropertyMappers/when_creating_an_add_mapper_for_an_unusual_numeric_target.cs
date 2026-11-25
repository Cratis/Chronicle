// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_creating_an_add_mapper_for_an_unusual_numeric_target : Specification
{
    Exception _creationException;
    PropertyDifference _difference;

    void Because()
    {
        PropertyMapper<Cratis.Chronicle.Concepts.Events.AppendedEvent, ExpandoObject> mapper = null!;
        _creationException = Catch.Exception(() => mapper = PropertyMappers.AddWithEventValueProvider(new TypeFormats(), new PropertyPath("total"), new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "uint32" }, _ => 3));
        var target = new ExpandoObject();
        ((IDictionary<string, object?>)target)["total"] = 5u;
        _difference = mapper(given.an_appended_event.with_content(new ExpandoObject()), target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_not_throw_when_creating_the_mapper() => _creationException.ShouldBeNull();
    [Fact] void should_add_through_the_fallback_conversion() => _difference.Changed.ShouldEqual(8u);
}
