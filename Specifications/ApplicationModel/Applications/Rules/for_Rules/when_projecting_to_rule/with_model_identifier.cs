// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Strings;

namespace Aksio.Cratis.Applications.Rules.for_Rules.when_checking_if_has_for_type.when_projecting_to_rule;

public class with_model_identifier : given.no_rules
{
    const string first_state_value = "Forty two";
    const int second_state_value = 42;
    const int complex_state_some_integer = 43;
    const string complex_state_some_string = "Forty three";

    const string model_identifier = "282c491b-10a9-4ec0-ae23-659c4e6aaf16";

    RuleWithState rule;
    ProjectionId projection_id;
    ImmediateProjectionKey key;
    Mock<IImmediateProjection> projection;

    void Establish()
    {
        rule = new();
        projection = new();
        var jsonObject = new JsonObject
        {
            [nameof(RuleWithState.FirstStateValue).ToCamelCase()] = first_state_value,
            [nameof(RuleWithState.SecondStateValue).ToCamelCase()] = second_state_value,
            [nameof(RuleWithState.ComplexState).ToCamelCase()] = new JsonObject
            {
                [nameof(ComplexState.SomeInteger).ToCamelCase()] = complex_state_some_integer,
                [nameof(ComplexState.SomeString).ToCamelCase()] = complex_state_some_string
            }
        };
        projection.Setup(_ => _.GetModelInstance(IsAny<ProjectionDefinition>())).Returns(Task.FromResult(jsonObject));

        cluster_client
            .Setup(_ => _.GetGrain<IImmediateProjection>(IsAny<Guid>(), IsAny<string>(), null))
            .Returns((Guid id, string keyExtension, string _) =>
            {
                projection_id = id;
                key = ImmediateProjectionKey.Parse(keyExtension);

                return projection.Object;
            });
    }

    void Because() => rules.ProjectTo(rule, model_identifier);

    [Fact] void should_use_microservice_id_on_key() => key.MicroserviceId.ShouldEqual(execution_context.MicroserviceId);
    [Fact] void should_use_tenant_id_on_key() => key.TenantId.ShouldEqual(execution_context.TenantId);
    [Fact] void should_use_model_identifier_on_key() => key.ModelKey.ShouldEqual((ModelKey)model_identifier);
    [Fact] void should_set_first_state_value() => rule.FirstStateValue.ShouldEqual(first_state_value);
    [Fact] void should_set_second_state_value() => rule.SecondStateValue.ShouldEqual(second_state_value);
    [Fact] void should_set_complex_state() => rule.ComplexState.ShouldEqual(new ComplexState(complex_state_some_integer, complex_state_some_string));
}
