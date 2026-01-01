// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_AstToProjectionDefinitionCompiler;

public class when_compiling_simple_projection : Specification
{
    const string Dsl = "projection MyProjection => Users\n\ton event UserRegistered\n\t\tkey = userId\n\t\tname = name\n\t\temail = email";

    ProjectionDefinition _result;

    void Because() => _result = Cratis.Chronicle.Projections.DSL.ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_have_users_as_read_model_name() => _result.ReadModel.Value.ShouldEqual("Users");
    [Fact] void should_have_one_event_type() => _result.From.Count.ShouldEqual(1);
    [Fact] void should_have_user_registered_event_type() => _result.From.First().Key.Id.Value.ShouldEqual("UserRegistered");
    [Fact] void should_set_key_to_user_id() => _result.From.First().Value.Key.Value.ShouldEqual("userId");
    [Fact] void should_map_name_property() => _result.From.First().Value.Properties[new PropertyPath("name")].ShouldEqual("name");
    [Fact] void should_map_email_property() => _result.From.First().Value.Properties[new PropertyPath("email")].ShouldEqual("email");
}
