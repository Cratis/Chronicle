// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class projection_block_support_is_exhaustive : for_LanguageService.given.a_language_service
{
    static readonly IReadOnlyDictionary<Type, string> _fixtures = new Dictionary<Type, string>
    {
        [typeof(FromSyntax)] = "from TestEvent",
        [typeof(EverySyntax)] = "every\n  id = $eventSourceId",
        [typeof(AllSyntax)] = "all\n  id = $eventSourceId",
        [typeof(JoinSyntax)] = "join Test on id\n  with TestEvent",
        [typeof(ChildrenSyntax)] = "children entries identified by id\n  from TestEvent",
        [typeof(NestedSyntax)] = "nested inner\n  from TestEvent",
        [typeof(RemoveWithSyntax)] = "remove with TestEvent",
        [typeof(RemoveViaJoinSyntax)] = "remove via join on TestEvent",
        [typeof(ClearWithSyntax)] = "clear with TestEvent",
        [typeof(ProjectionVariantSyntax)] = "variant CompanyReadModel\n  enters on TestEvent"
    };

    [Fact]
    void should_classify_every_concrete_projection_block()
    {
        var concrete = typeof(ProjectionBlockSyntax).Assembly.GetTypes()
            .Where(_ => _.IsSubclassOf(typeof(ProjectionBlockSyntax)) && !_.IsAbstract)
            .ToHashSet();
        concrete.SetEquals(ProjectionValidator.ProjectionBlockSupport.Keys).ShouldBeTrue();
        concrete.SetEquals(_fixtures.Keys).ShouldBeTrue();
    }

    [Fact]
    void should_compile_supported_and_reject_unsupported_blocks_at_every_level()
    {
        foreach (var (syntaxType, snippet) in _fixtures)
        {
            foreach (var level in Enum.GetValues<ProjectionLevel>())
            {
                var declaration = DeclarationFor(level, snippet);
                var result = _languageService.Compile(declaration, ProjectionOwner.Client, [], []);
                var errors = result.Match(_ => CompilerErrors.Empty, _ => _);
                var expected = ProjectionValidator.ProjectionBlockSupport[syntaxType].Contains(level);
                if (expected)
                {
                    Assert.False(errors.HasErrors, $"{syntaxType.Name} at {level}: {string.Join(", ", errors.Errors.Select(_ => _.Message))}");
                }
                else
                {
                    Assert.True(errors.HasErrors, $"{syntaxType.Name} at {level} was silently accepted");
                }
            }
        }
    }

    static string DeclarationFor(ProjectionLevel level, string snippet)
    {
        var block = Indent(snippet, level == ProjectionLevel.Root ? 2 : 4);
        return level switch
        {
            ProjectionLevel.Root => $"projection Test => CompanyReadModel\n{block}",
            ProjectionLevel.Children => $"projection Test => CompanyReadModel\n  children departments identified by id\n    from DepartmentCreated\n{block}",
            _ => $"projection Test => CompanyReadModel\n  nested details\n    from DepartmentCreated\n{block}"
        };
    }

    static string Indent(string snippet, int spaces) => string.Join('\n', snippet.Split('\n').Select(_ => new string(' ', spaces) + _));
}
