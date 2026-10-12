// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_SetBuilder.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_setting_a_nested_target;

public class and_a_single_member_is_supplied_in_lower_case : Specification
{
    FromBuilder<NestedReadModel, NestedEvent, object> _builder;
    PropertyPath _target;

    void Establish() => _builder = new(Substitute.For<IProjectionBuilder<NestedReadModel, object>>(), new DefaultNamingPolicy());
    void Because() => _target = _builder.Set("meta").TargetProperty;

    [Fact] void should_preserve_single_member_name_resolution() => _target.Path.ShouldEqual("Meta");
}
