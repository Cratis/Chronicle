// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_SetBuilder.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_setting_a_nested_target;

public class and_the_accessor_is_a_path : Specification
{
    FromBuilder<NestedReadModel, NestedEvent, object> _builder;
    PropertyPath _target;

    void Establish() => _builder = new(Substitute.For<IProjectionBuilder<NestedReadModel, object>>(), new CamelCaseNamingPolicy());
    void Because() => _target = _builder.Set("Meta.Topic").TargetProperty;

    [Fact] void should_preserve_and_name_every_target_segment() => _target.Path.ShouldEqual("meta.topic");
}
