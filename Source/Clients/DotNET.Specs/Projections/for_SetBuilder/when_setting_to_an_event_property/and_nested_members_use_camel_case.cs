// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_SetBuilder.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_property;

public class and_nested_members_use_camel_case : Specification
{
    SetBuilder<object, NestedEvent, string, object> _builder;

    void Establish() => _builder = new(new object(), "topic", new CamelCaseNamingPolicy());
    void Because() => _builder.To(e => e.Brief.Topic);

    [Fact] void should_name_every_segment_as_serialized() => _builder.Build().ShouldEqual("brief.topic");
}
