// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_getting_read_model_identifier;

public class with_event_context_paths_chronicle_resolves : a_language_service
{
    const string Declaration = """
        projection Activity => ActivityReadModel
          from ActivityLogged
            source = $eventContext.eventSource
            year = $eventContext.occurred.year
        """;

    ReadModelIdentifier? _identifier;

    void Because() => _identifier = _languageService.GetReadModelIdentifier(Declaration).Match<ReadModelIdentifier?>(identifier => identifier, _ => null);

    [Fact] void should_resolve_the_read_model_identifier() => _identifier.ShouldEqual(new ReadModelIdentifier("ActivityReadModel"));
}
