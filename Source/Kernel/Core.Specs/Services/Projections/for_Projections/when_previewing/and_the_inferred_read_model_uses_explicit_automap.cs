// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_the_inferred_read_model_uses_explicit_automap : given.an_inferred_read_model_preview
{
    OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors> _result;

    async Task Because() => _result = await _service.Preview(new PreviewProjectionRequest
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = """
            projection PreviewIssues
              automap
              from VariantIssueOpened
            """
    });

    [Fact] void should_infer_the_projected_property_schema() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties.ContainsKey("title").ShouldBeTrue();
    [Fact] void should_preserve_the_projected_property_in_the_response() => (JsonNode.Parse(_result.Value0.ReadModelEntries.Single())!["title"]?.GetValue<string>()).ShouldEqual("preview");
}
