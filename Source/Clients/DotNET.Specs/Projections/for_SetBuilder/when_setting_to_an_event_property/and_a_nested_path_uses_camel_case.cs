// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_property;

public class and_a_nested_path_uses_camel_case : Specification
{
    SetBuilder<object, object, object> _builder;

    void Establish() => _builder = new(new object(), "topic", new CamelCaseNamingPolicy());
    void Because() => _builder.To("Brief.Topic");

    [Fact] void should_name_every_segment_as_serialized() => _builder.Build().ShouldEqual("brief.topic");
}
