// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_evaluating_read_model_migrations;

/// <summary>
/// The counterpart, and the one that keeps the recommendation from collapsing into "replay whenever an enum is
/// added". A nullable enum has an answer for every existing row - the absence of one - so an added generation
/// needs no events re-read, and the operator is told so.
/// <para>
/// Nullability here is the <c>"null"</c> entry in the property's type rather than a trailing <c>?</c> on a
/// format, because an enum carries no format. Read only the format and this property looks required, the 1-based
/// member list withholds its default, and the operator is sent to a replay the data never needed.
/// </para>
/// </summary>
public class and_added_properties_are_nullable_enums : given.a_projection_replay_recommendation_evaluator
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
                "rejectionReason": {
                  "type": ["integer", "null"],
                  "enum": [1, 2, 3],
                  "x-enumNames": ["RejectedBySigner", "Expired", "Withdrawn"]
                }
              }
            }
            """);

        var readModelDefinition = CreateReadModelDefinition(previousSchema, currentSchema);

        _result = ProjectionReplayRecommendationEvaluator.GetReadModelMigrationRecommendation(readModelDefinition);
    }

    [Fact] void should_recommend_update_available() => _result.ShouldEqual(ProjectionReadModelMigrationRecommendation.UpdateAvailable);
}
