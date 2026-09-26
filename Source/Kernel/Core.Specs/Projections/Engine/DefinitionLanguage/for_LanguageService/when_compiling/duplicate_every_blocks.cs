// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class duplicate_every_blocks : for_LanguageService.given.a_language_service
{
    CompilerErrors _twoEvery;
    CompilerErrors _everyAndAll;
    CompilerErrors _childEvery;

    void Because()
    {
        _twoEvery = Errors("""
            projection Test => CompanyReadModel
              every
                id = $eventSourceId
              every
                id = $eventSourceId
            """);
        _everyAndAll = Errors("""
            projection Test => CompanyReadModel
              every
                id = $eventSourceId
              all
                id = $eventSourceId
            """);
        _childEvery = Errors("""
            projection Test => CompanyReadModel
              children departments identified by id
                from DepartmentCreated
                every
                  id = $eventSourceId
                every
                  id = $eventSourceId
            """);
    }

    CompilerErrors Errors(string declaration) => _languageService.Compile(declaration, ProjectionOwner.Client, [], [])
        .Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_two_every_blocks() => _twoEvery.Errors.ShouldContain(_ => _.Message.Contains("Only one 'every' or 'all' block", StringComparison.Ordinal));
    [Fact] void should_reject_every_and_all() => _everyAndAll.Errors.ShouldContain(_ => _.Message.Contains("Only one 'every' or 'all' block", StringComparison.Ordinal));
    [Fact] void should_reject_two_every_blocks_in_children() => _childEvery.Errors.ShouldContain(_ => _.Message.Contains("Only one 'every' or 'all' block", StringComparison.Ordinal));
}
