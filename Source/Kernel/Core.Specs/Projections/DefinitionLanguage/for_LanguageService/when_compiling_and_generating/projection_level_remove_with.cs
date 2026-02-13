// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_level_remove_with : given.a_language_service_with_schemas<given.GroupReadModel>
{
    const string Declaration = """
        projection Group => GroupReadModel
          from GroupCreated automap

          remove with GroupDeleted
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.GroupCreated), typeof(given.GroupDeleted)];

    ProjectionDefinition _result;
    RemovedWithDefinition _removedWithDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Declaration);
        _removedWithDef = _result.RemovedWith.Values.FirstOrDefault();
    }

    [Fact] void should_have_removed_with_definition() => _removedWithDef.ShouldNotBeNull();
    [Fact] void should_have_one_removed_with_entry() => _result.RemovedWith.Count.ShouldEqual(1);
    [Fact] void should_have_key_as_not_set() => _removedWithDef.Key.IsSet().ShouldBeFalse();
    [Fact] void should_not_have_parent_key() => _removedWithDef.ParentKey.ShouldBeNull();
}
