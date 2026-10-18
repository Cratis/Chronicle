// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_event_context_paths;

public class and_they_are_unknown : given.a_language_service
{
    CompilerErrors _errors;

    void Because() => _errors = _languageService.Compile(
            """
            projection Activity => ActivityReadModel
              from ActivityLogged
                createdByName = $eventContext.whatever
                correlationId = $eventContext.occurred.whatever
                position = $eventContext.occurred.date.whatever
            """,
            Concepts.Projections.ProjectionOwner.Client,
            [],
            [])
        .Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_the_unknown_member() => _errors.Errors.Any(error => error.Line == 3 && error.Message.Contains("Unknown event context member 'whatever'")).ShouldBeTrue();
    [Fact] void should_reject_the_unknown_path() => _errors.Errors.Any(error => error.Line == 4 && error.Message.Contains("Unknown event context path")).ShouldBeTrue();
    [Fact] void should_reject_the_unknown_nested_path() => _errors.Errors.Any(error => error.Line == 5 && error.Message.Contains("Unknown event context path")).ShouldBeTrue();
}
