// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_the_inferred_declaration_has_an_unknown_event : given.an_inferred_read_model_preview
{
    OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors> _result;

    async Task Because() => _result = await _service.Preview(new PreviewProjectionRequest
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = """
            projection PreviewIssues
              from UnknownEvent
            """
    });

    [Fact] void should_return_diagnostics() => _result.Value1.Errors.ShouldNotBeEmpty();
}
