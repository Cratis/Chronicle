// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_setting;

public class and_a_single_member_is_named_like_a_function : Specification
{
    FromBuilder<CalendarModel, CalendarModel, object> _builder;
    PropertyPath _target;

    void Establish() => _builder = new(Substitute.For<IProjectionBuilder<CalendarModel, object>>(), new CamelCaseNamingPolicy());
    void Because() => _target = _builder.Set(model => model.Week).TargetProperty;

    [Fact] void should_name_the_target_member() => _target.Path.ShouldEqual("week");
    [Fact] void should_resolve_a_member() => _target.LastSegment.ShouldBeOfExactType<PropertyName>();
}
