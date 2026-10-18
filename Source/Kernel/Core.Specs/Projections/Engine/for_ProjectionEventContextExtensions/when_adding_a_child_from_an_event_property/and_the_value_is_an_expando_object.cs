// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_adding_a_child_from_an_event_property;

public class and_the_value_is_an_expando_object : given.a_projection_event_context_observable
{
    static readonly PropertyPath _children = new("items");

    ExpandoObject _content;
    object _child;

    void Establish()
    {
        _content = new ExpandoObject();
        ((IDictionary<string, object?>)_content)["name"] = "value";
        _eventContext = _eventContext with { Event = _eventContext.Event with { Content = _content } };
        _eventContext.Changeset.When(_ => _.AddChild(Arg.Any<PropertyPath>(), Arg.Any<object>()))
            .Do(call => _child = call.ArgAt<object>(1));
        _subject.AddChildFromEventProperty(_children, @event => @event.Content).Subscribe();
    }

    void Because() => _subject.OnNext(_eventContext);

    [Fact] void should_add_the_child_to_the_changeset() => _child.ShouldNotBeNull();
    [Fact] void should_not_add_the_event_content_instance() => _child.ShouldNotBeSame(_content);
    [Fact] void should_copy_the_values() => ((IDictionary<string, object?>)_child)["name"].ShouldEqual("value");
}
