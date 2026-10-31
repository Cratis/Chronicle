// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object;

public class and_the_naming_policy_is_camel_case : Specification
{
    FromBuilder<NamedChild, NamedChildEvent, object> _from;
    AddChildBuilder<object, NamedChild, NamedChildEvent> _builder;

    void Establish()
    {
        var policy = new CamelCaseNamingPolicy();
        _from = new(Substitute.For<IProjectionBuilder<NamedChild, object>>(), policy);
        _builder = new(Substitute.For<IChildrenBuilder<object, NamedChild>>(), _from, policy);
    }

    void Because() => _builder.FromObject(@event => @event.TheChild);

    [Fact] void should_name_the_target_member() => _from.Build().Properties.Keys.ShouldContain("firstName");
    [Fact] void should_name_the_source_member() => _from.Build().Properties["firstName"].ShouldEqual("theChild.firstName");
}
