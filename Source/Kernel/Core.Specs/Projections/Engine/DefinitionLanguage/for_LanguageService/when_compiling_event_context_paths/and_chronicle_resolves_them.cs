// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_event_context_paths;

/// <summary>
/// Screenplay's event context catalog is deliberately incomplete and only warns on a path it does not list;
/// Chronicle resolves event context paths by reflecting over its own event context at runtime.
/// </summary>
public class and_chronicle_resolves_them : given.a_language_service
{
    CompilerErrors _errors;

    void Because() => _errors = _languageService.Compile(
            """
            projection Activity => ActivityReadModel
              from ActivityLogged
                source = $eventContext.eventSource
                generation = $eventContext.appendedGeneration
                tags = $eventContext.namedTags
                year = $eventContext.occurred.year
                day = $eventContext.occurred.dayOfWeek
                offset = $eventContext.occurred.offset
                week = $eventContext.occurred.Week
            """,
            Concepts.Projections.ProjectionOwner.Client,
            [],
            [])
        .Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_not_have_errors() => _errors.HasErrors.ShouldBeFalse();
}
