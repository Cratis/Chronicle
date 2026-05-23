// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_evaluating_read_model_migrations;

public class and_added_properties_do_not_have_default_values : given.a_projection_replay_recommendation_evaluator
{
    ProjectionReadModelMigrationRecommendation _result;

    void Establish()
    {
        var previousSchema = JsonSchema.FromJson("""
            {
              "type": "object",
              "properties": {
                "name": { "type": "string" }
              }
            }
            """);

        var currentSchema = JsonSchema.FromJson("""
            {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "nickname": { "type": "string" }
              }
            }
            """);

        var readModelDefinition = CreateReadModelDefinition(previousSchema, currentSchema);

        _result = ProjectionReplayRecommendationEvaluator.GetReadModelMigrationRecommendation(readModelDefinition);
    }

    [Fact] void should_recommend_selective_replay_available() => _result.ShouldEqual(ProjectionReadModelMigrationRecommendation.SelectiveReplayAvailable);
}
