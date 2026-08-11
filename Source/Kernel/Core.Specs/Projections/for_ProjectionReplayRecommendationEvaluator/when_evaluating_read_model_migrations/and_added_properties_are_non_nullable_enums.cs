// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_evaluating_read_model_migrations;

/// <summary>
/// The recommendation is a value an operator sees and acts on, and withholding illegal defaults moved this case
/// across the line: a required 1-based enum used to look like a property with a type default in hand, so a
/// generation bump was reported as a plain update. It is not one. There is no value the migration may write for
/// this property, so the rows have to be re-derived from the events that own them.
/// <para>
/// Every other spec here adds a text property, which never reaches the member-list decision at all - so
/// the whole flip from update to selective replay happened without a single spec noticing.
/// </para>
/// </summary>
public class and_added_properties_are_non_nullable_enums : given.a_projection_replay_recommendation_evaluator
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
                "status": {
                  "type": "integer",
                  "enum": [1, 2, 3],
                  "x-enumNames": ["Draft", "Signed", "Terminated"]
                }
              }
            }
            """);

        var readModelDefinition = CreateReadModelDefinition(previousSchema, currentSchema);

        _result = ProjectionReplayRecommendationEvaluator.GetReadModelMigrationRecommendation(readModelDefinition);
    }

    [Fact] void should_recommend_selective_replay_available() => _result.ShouldEqual(ProjectionReadModelMigrationRecommendation.SelectiveReplayAvailable);
}
