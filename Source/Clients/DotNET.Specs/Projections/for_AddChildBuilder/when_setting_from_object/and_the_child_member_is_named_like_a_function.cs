// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object.given;
using Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object;

public class and_the_child_member_is_named_like_a_function : Specification
{
    FromBuilder<CalendarModel, CalendarChildEvent, object> _from;
    AddChildBuilder<object, CalendarModel, CalendarChildEvent> _builder;

    void Establish()
    {
        var policy = new CamelCaseNamingPolicy();
        _from = new(Substitute.For<IProjectionBuilder<CalendarModel, object>>(), policy);
        _builder = new(Substitute.For<IChildrenBuilder<object, CalendarModel>>(), _from, policy);
    }

    void Because() => _builder.FromObject(@event => @event.Child);

    [Fact] void should_name_the_target_member() => _from.Build().Properties.Keys.ShouldContain("week");
    [Fact] void should_name_the_source_member() => _from.Build().Properties["week"].ShouldEqual("child.week");
}
