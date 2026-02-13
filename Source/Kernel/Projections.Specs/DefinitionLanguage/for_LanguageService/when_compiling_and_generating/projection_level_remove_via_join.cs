// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_level_remove_via_join : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          from UserRegistered automap

          join Group on GroupId
            with GroupCreated
              automap

          remove via join on GroupDeleted
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserRegistered), typeof(given.GroupCreated), typeof(given.GroupDeleted)];

    ProjectionDefinition _result;
    RemovedWithJoinDefinition _removedWithJoinDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Declaration);
        _removedWithJoinDef = _result.RemovedWithJoin.Values.FirstOrDefault();
    }

    [Fact] void should_have_removed_with_join_definition() => _removedWithJoinDef.ShouldNotBeNull();
    [Fact] void should_have_one_removed_with_join_entry() => _result.RemovedWithJoin.Count.ShouldEqual(1);
}
