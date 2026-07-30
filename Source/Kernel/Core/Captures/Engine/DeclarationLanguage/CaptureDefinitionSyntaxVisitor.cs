// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Visits a Screenplay <see cref="CaptureSyntax"/> tree and produces the corresponding <see cref="CaptureDefinition"/>.
/// </summary>
internal class CaptureDefinitionSyntaxVisitor : ICaptureSyntaxVisitor<CaptureDefinition>
{
    /// <inheritdoc/>
    public CaptureDefinition Visit(CaptureSyntax syntax)
    {
        if (syntax.Source is null || syntax.Key is null)
        {
            throw new UnsupportedCaptureSyntax(syntax);
        }

        return new CaptureDefinition(
            CaptureId.NotSet,
            syntax.Name,
            ConvertSource(syntax.Source),
            syntax.Key,
            ConvertMap(syntax.Map),
            syntax.Appends.Select(ConvertAppend).ToList(),
            syntax.Nested.Select(ConvertNested).ToList(),
            syntax.Children.Select(ConvertChildren).ToList());
    }

    static SourceDefinition ConvertSource(CaptureSourceSyntax source) => new(
        ConvertSourceType(source),
        Api: GetSetting(source, "api"),
        Poll: GetSetting(source, "poll"),
        Route: GetSetting(source, "route"),
        Path: GetSetting(source, "path"),
        Topic: GetSetting(source, "topic"));

    static SourceType ConvertSourceType(CaptureSourceSyntax source) => source.Kind switch
    {
        "api" => SourceType.Api,
        "webhook" => SourceType.Webhook,
        "message" => SourceType.Message,
        _ => throw new UnsupportedCaptureSyntax(source)
    };

    static string? GetSetting(CaptureSourceSyntax source, string name) =>
        source.Settings.FirstOrDefault(setting => setting.Name == name)?.Value;

    static MapDefinition? ConvertMap(IEnumerable<CaptureMapOperationSyntax> operations)
    {
        var list = operations.Select(ConvertMapOperation).ToList();

        return list.Count > 0 ? new MapDefinition(list) : null;
    }

    static MapOperation ConvertMapOperation(CaptureMapOperationSyntax operation) => operation switch
    {
        CaptureSplitSyntax split => new SplitOperation(ConvertExpressionToString(split.Source), split.Separator, split.Targets.ToList()),
        CaptureMapEntrySyntax entry when entry.Translations.Any() => new TranslateOperation(
            entry.Property,
            ConvertExpressionToString(entry.Source),
            entry.Translations.Select(translation => new TranslateValue(translation.From, translation.To)).ToList()),
        CaptureMapEntrySyntax { Source: TemplateExpressionSyntax template } entry => new TemplateAssignOperation(entry.Property, ConvertTemplateToString(template)),
        CaptureMapEntrySyntax entry => new FieldRenameOperation(ConvertExpressionToString(entry.Source), entry.Property),
        _ => throw new UnsupportedCaptureSyntax(operation)
    };

    static AppendDefinition ConvertAppend(CaptureAppendSyntax append)
    {
        if (append.When is null)
        {
            throw new UnsupportedCaptureSyntax(append);
        }

        return new AppendDefinition(
            append.Event,
            ConvertWhen(append.When),
            append.Mappings.ToDictionary(mapping => mapping.Property, mapping => ConvertExpressionToString(mapping.Source)));
    }

    static WhenClause ConvertWhen(CaptureWhenSyntax when) => new(
        ConvertWhenType(when),
        when.Properties.ToList(),
        when.FromValue,
        when.ToValue,
        StripTemplateDelimiters(when.Expression));

    static WhenClauseType ConvertWhenType(CaptureWhenSyntax when) => when.Kind switch
    {
        CaptureWhenKind.PropertyChanged => WhenClauseType.PropertyChange,
        CaptureWhenKind.Added => WhenClauseType.Added,
        CaptureWhenKind.Removed => WhenClauseType.Removed,
        CaptureWhenKind.LogicalOr => WhenClauseType.LogicalOr,
        CaptureWhenKind.LogicalAnd => WhenClauseType.LogicalAnd,
        CaptureWhenKind.ValueTransition => WhenClauseType.ValueTransition,
        CaptureWhenKind.Expression => WhenClauseType.Expression,

        // CaptureWhenKind.Changed predates the reconciliation of Screenplay's Capture syntax with Chronicle's
        // Capture Declaration Language grammar and has no corresponding WhenClauseType - the parser never
        // produces it from CDL text, so this is unreachable in practice, but it is not silently dropped.
        _ => throw new UnsupportedCaptureSyntax(when)
    };

    static NestedDefinition ConvertNested(CaptureNestedSyntax nested) => new(
        nested.Property,
        ConvertMap(nested.Map),
        nested.Appends.Select(ConvertAppend).ToList());

    static ChildrenDefinition ConvertChildren(CaptureChildrenSyntax children) => new(
        children.Property,
        children.IdentifiedBy,
        ConvertMap(children.Map),
        children.Appends.Select(ConvertAppend).ToList());

    static string ConvertExpressionToString(ExpressionSyntax expression) => expression switch
    {
        PathExpressionSyntax path => path.Path,
        SourceItemExpressionSyntax item => $"$.{item.Path}",
        ContextExpressionSyntax context => $"$context.{context.Path}",
        EnvironmentExpressionSyntax env => $"$env.{env.Name}",
        EventSourceIdExpressionSyntax => WellKnownExpressions.EventSourceId,
        EventContextExpressionSyntax eventContext => $"{WellKnownExpressions.EventContext}({eventContext.Path})",
        CausedByExpressionSyntax causedBy => causedBy.Property is null
            ? WellKnownExpressions.CausedBy
            : $"{WellKnownExpressions.CausedBy}({causedBy.Property})",
        LiteralExpressionSyntax literal => FormatLiteralForStorage(literal.Value),
        TemplateExpressionSyntax template => $"`{ConvertTemplateToString(template)}`",
        RawExpressionSyntax raw => raw.Text,
        _ => throw new UnsupportedCaptureSyntax(expression)
    };

    static string ConvertTemplateToString(TemplateExpressionSyntax template)
    {
        var builder = new StringBuilder();
        foreach (var part in template.Parts)
        {
            switch (part)
            {
                case TemplateTextSyntax text:
                    builder.Append(text.Text);
                    break;
                case TemplateInterpolationSyntax interpolation:
                    builder
                        .Append("${")
                        .Append(ConvertExpressionToString(interpolation.Expression))
                        .Append('}');
                    break;
            }
        }

        return builder.ToString();
    }

    static string FormatLiteralForStorage(object? value) =>
        value switch
        {
            null => string.Empty, // Null is stored as an empty string
            string text => $"\"{text}\"", // Strings keep their quotes to distinguish them from property names
            bool boolean => boolean.ToString(), // Stored as "True"/"False"
            double number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    static string? StripTemplateDelimiters(string? expression) =>
        expression is { Length: >= 2 } && expression.StartsWith('`') && expression.EndsWith('`')
            ? expression[1..^1]
            : expression;
}
