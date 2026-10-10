// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Checks the keys, mappings and expressions before the definition visitor lowers them, even without schemas.
/// </summary>
/// <param name="errors">The compiler errors to report unsupported syntax to.</param>
internal sealed class ProjectionSyntaxSupport(CompilerErrors errors) : ScreenplaySyntaxWalker
{
    /// <summary>
    /// Gets the exhaustive classification of the pinned Screenplay nodes. A null reason means the
    /// definition visitor handles the type; a non-null reason explains why Chronicle rejects it.
    /// </summary>
    internal static IReadOnlyDictionary<Type, string?> NodeSupport { get; } = new Dictionary<Type, string?>
    {
        [typeof(ExpressionKeySyntax)] = null,
        [typeof(CompositeKeySyntax)] = null,
        [typeof(SetMappingSyntax)] = null,
        [typeof(ClearMappingSyntax)] = null,
        [typeof(AddMappingSyntax)] = null,
        [typeof(SubtractMappingSyntax)] = null,
        [typeof(IncrementMappingSyntax)] = null,
        [typeof(DecrementMappingSyntax)] = null,
        [typeof(CountMappingSyntax)] = null,
        [typeof(PathExpressionSyntax)] = null,
        [typeof(EventContextExpressionSyntax)] = null,
        [typeof(EventSourceIdExpressionSyntax)] = null,
        [typeof(CausedByExpressionSyntax)] = null,
        [typeof(LiteralExpressionSyntax)] = null,
        [typeof(TemplateExpressionSyntax)] = null,
        [typeof(RawExpressionSyntax)] = null,
        [typeof(TemplateTextSyntax)] = null,
        [typeof(TemplateInterpolationSyntax)] = null,
        [typeof(RefusalExpressionSyntax)] = "Refusal values belong to reactions, not stored event projections",
        [typeof(ListExpressionSyntax)] = "Inline lists have no projection property expression lowering",
        [typeof(ObjectExpressionSyntax)] = "Inline objects have no projection property expression lowering",
        [typeof(CaseValueExpressionSyntax)] = "Case parameters belong to specifications, not stored event projections",
        [typeof(ContextExpressionSyntax)] = "Command and query contexts are not event contexts; use $eventContext",
        [typeof(EnvironmentExpressionSyntax)] = "Environment values are not stored event data",
        [typeof(StringsExpressionSyntax)] = "Localized strings are not stored event data",
        [typeof(SourceItemExpressionSyntax)] = "Source items belong to captures, not stored event projections"
    };

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is not (MappingSyntax or KeySyntax or ExpressionSyntax or TemplatePartSyntax))
        {
            return;
        }

        if (!NodeSupport.TryGetValue(node.GetType(), out var reason))
        {
            reason = "No projection definition lowering is available";
        }

        if (reason is not null)
        {
            errors.Add($"Projection syntax of type '{node.GetType().Name}' is not supported: {reason}", node.Location.Line, node.Location.Column);
        }
    }
}
