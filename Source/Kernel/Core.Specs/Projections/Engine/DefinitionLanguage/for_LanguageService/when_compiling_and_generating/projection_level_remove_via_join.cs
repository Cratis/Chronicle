// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_level_remove_via_join : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          from UserRegistered

          join Group on GroupId
            with GroupCreated
              automap

          remove via join on GroupDeleted
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserRegistered), typeof(given.GroupCreated), typeof(given.GroupDeleted)];

    CompilerErrors _withSchemas;
    CompilerErrors _withoutSchemas;

    void Because()
    {
        _withSchemas = Errors(true);
        _withoutSchemas = Errors(false);
    }

    CompilerErrors Errors(bool schemas) => _languageService.Compile(
        Declaration,
        ProjectionOwner.Client,
        schemas ? [_readModelDefinition] : [],
        schemas ? _eventTypeSchemas : []).Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_reject_root_removal_with_schemas() => _withSchemas.Errors.ShouldContain(_ => _.Message.Contains("'remove via join' at the root is not supported", StringComparison.Ordinal) && _.Line == 8);
    [Fact] void should_reject_root_removal_without_schemas() => _withoutSchemas.Errors.ShouldContain(_ => _.Message.Contains("'remove via join' at the root is not supported", StringComparison.Ordinal) && _.Line == 8);
}
