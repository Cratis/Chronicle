// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_exhaustive_syntax_support : for_LanguageService.given.a_language_service
{
    static readonly SourceLocation _location = new(7, 5);
    static readonly IReadOnlyDictionary<Type, string> _supported = new Dictionary<Type, string>
    {
        [typeof(ExpressionKeySyntax)] = "from DocEvent\n  key id",
        [typeof(CompositeKeySyntax)] = "from DocEvent\n  key DocKey\n    id = id",
        [typeof(SetMappingSyntax)] = "from DocEvent\n  name = name",
        [typeof(ClearMappingSyntax)] = "from DocEvent\n  clear name",
        [typeof(AddMappingSyntax)] = "from DocEvent\n  add total by amount",
        [typeof(SubtractMappingSyntax)] = "from DocEvent\n  subtract total by amount",
        [typeof(IncrementMappingSyntax)] = "from DocEvent\n  increment total",
        [typeof(DecrementMappingSyntax)] = "from DocEvent\n  decrement total",
        [typeof(CountMappingSyntax)] = "from DocEvent\n  count total",
        [typeof(PathExpressionSyntax)] = "from DocEvent\n  name = name",
        [typeof(EventContextExpressionSyntax)] = "from DocEvent\n  updated = $eventContext.occurred",
        [typeof(EventSourceIdExpressionSyntax)] = "from DocEvent\n  id = $eventSourceId",
        [typeof(CausedByExpressionSyntax)] = "from DocEvent\n  name = $causedBy.name",
        [typeof(LiteralExpressionSyntax)] = "from DocEvent\n  name = \"test\"",
        [typeof(TemplateExpressionSyntax)] = "from DocEvent\n  name = `Hello ${name}`",
        [typeof(RawExpressionSyntax)] = "from DocEvent\n  name = $custom(name)",
        [typeof(TemplateTextSyntax)] = "from DocEvent\n  name = `Hello ${name}`",
        [typeof(TemplateInterpolationSyntax)] = "from DocEvent\n  name = `Hello ${name}`"
    };

    static readonly ExpressionSyntax[] _rejected =
    [
        new RefusalExpressionSyntax("reason", _location),
        new ListExpressionSyntax([], _location),
        new ObjectExpressionSyntax([], _location),
        new CaseValueExpressionSyntax("value", _location),
        new ContextExpressionSyntax("tenant", _location),
        new EnvironmentExpressionSyntax("NAME", _location),
        new StringsExpressionSyntax("name", _location),
        new SourceItemExpressionSyntax("name", _location)
    ];

    [Fact]
    void should_classify_every_concrete_mapping_key_expression_and_template_part()
    {
        Type[] families = [typeof(MappingSyntax), typeof(KeySyntax), typeof(ExpressionSyntax), typeof(TemplatePartSyntax)];
        var concrete = typeof(ExpressionSyntax).Assembly.GetTypes().Where(type => !type.IsAbstract && families.Any(type.IsSubclassOf)).ToHashSet();
        concrete.SetEquals(ProjectionSyntaxSupport.NodeSupport.Keys).ShouldBeTrue();
        concrete.SetEquals(_supported.Keys.Concat(_rejected.Select(node => node.GetType()))).ShouldBeTrue();
        ProjectionSyntaxSupport.NodeSupport.Where(entry => entry.Value is not null).All(entry => !string.IsNullOrWhiteSpace(entry.Value)).ShouldBeTrue();
    }

    [Fact]
    void should_lower_every_supported_node()
    {
        foreach (var (type, snippet) in _supported)
        {
            var source = "projection Doc => DocReadModel\n" + string.Join('\n', snippet.Split('\n').Select(line => "  " + line));
            var syntax = new ScreenplayCompiler().CompileProjection(source).Value!;
            var nodes = new collected_nodes();
            nodes.VisitProjection(syntax);
            Assert.Contains(type, nodes.Types);
            ProjectionSyntaxSupport.NodeSupport[type].ShouldBeNull();
            var definition = new ProjectionDefinitionSyntaxVisitor(ProjectionOwner.Client).Visit(syntax);
            var result = _languageService.Compile(source, ProjectionOwner.Client, [], []);
            var errors = result.Match(_ => CompilerErrors.Empty, _ => _);

            // Screenplay creates a raw node as recovery for an invalid projection expression. The visitor
            // can still store it, but LanguageService must respect the parser's error rather than lower it.
            if (type == typeof(RawExpressionSyntax))
            {
                definition.From.Values.Single().Properties.Values.Single().ShouldEqual("$custom(name)");
                errors.HasErrors.ShouldBeTrue();
            }
            else
            {
                Assert.False(errors.HasErrors, $"{type.Name}: {string.Join("; ", errors.Errors.Select(error => error.Message))}");
            }
        }
    }

    [Fact]
    void should_reject_every_unsupported_expression_at_its_location()
    {
        foreach (var expression in _rejected)
        {
            var errors = new CompilerErrors([]);
            new ProjectionSyntaxSupport(errors).VisitExpression(expression);
            errors.HasErrors.ShouldBeTrue();
            errors.Errors.Single().Line.ShouldEqual(_location.Line);
            errors.Errors.Single().Column.ShouldEqual(_location.Column);
            ProjectionSyntaxSupport.NodeSupport[expression.GetType()].ShouldNotBeNull();
        }
    }

    [Fact]
    void should_report_unsupported_expressions_without_schemas_in_every_expression_position()
    {
        string[] snippets =
        [
            "from DocEvent key $refusal.reason",
            "from DocEvent\n  key $refusal.reason",
            "from DocEvent\n  key DocKey\n    id = $refusal.reason",
            "from DocEvent\n  parent $refusal.reason",
            "from DocEvent\n  name = $refusal.reason",
            "from DocEvent\n  add total by $refusal.reason",
            "from DocEvent\n  subtract total by $refusal.reason",
            "from DocEvent\n  name = `${$refusal.reason}`",
            "every\n  name = $refusal.reason",
            "all\n  name = $refusal.reason",
            "join Doc on id\n  with DocEvent\n    name = $refusal.reason",
            "children docs identified by $refusal.reason\n  from DocEvent",
            "children docs identified by id\n  from DocEvent\n    name = $refusal.reason",
            "nested doc\n  from DocEvent\n    name = $refusal.reason",
            "remove with DocEvent key $refusal.reason",
            "children docs identified by id\n  remove with DocEvent\n    parent $refusal.reason",
            "children docs identified by id\n  remove via join on DocEvent key $refusal.reason"
        ];
        foreach (var snippet in snippets)
        {
            var source = "projection Doc => DocReadModel\n" + string.Join('\n', snippet.Split('\n').Select(line => "  " + line));
            var result = _languageService.Compile(source, ProjectionOwner.Client, [], []);
            var errors = result.Match(_ => CompilerErrors.Empty, _ => _);
            Assert.True(errors.HasErrors && errors.Errors.Any(error => error.Message.Contains(nameof(RefusalExpressionSyntax), StringComparison.Ordinal)), $"Missing support diagnostic for {snippet}: {string.Join("; ", errors.Errors.Select(error => error.Message))}");
        }
    }

    sealed class collected_nodes : ScreenplaySyntaxWalker
    {
        internal HashSet<Type> Types { get; } = [];

        public override void VisitNode(SyntaxNode node) => Types.Add(node.GetType());
    }
}
