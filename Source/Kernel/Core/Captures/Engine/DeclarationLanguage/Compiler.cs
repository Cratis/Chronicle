// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Monads;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Compiles a capture AST into a <see cref="CaptureDefinition"/>.
/// </summary>
public class Compiler
{
    /// <summary>
    /// Compiles an AST document into a <see cref="CaptureDefinition"/>.
    /// </summary>
    /// <param name="document">The AST document to compile.</param>
    /// <returns>The compiled capture definition or compiler errors.</returns>
    public Result<CaptureDefinition, CompilerErrors> Compile(CaptureDocument document)
    {
        var documentValidation = document.Validate();
        if (!documentValidation.IsSuccess)
        {
            return CompilerErrors.From(documentValidation.AsT1);
        }

        var capture = document.Captures[0];
        var captureValidation = capture.Validate();
        if (!captureValidation.IsSuccess)
        {
            return CompilerErrors.From(captureValidation.AsT1);
        }

        var errors = new CompilerErrors();
        var sourceBlocks = capture.Directives.OfType<SourceBlock>().ToList();
        var keyDirectives = capture.Directives.OfType<KeyDirective>().ToList();
        var mapBlocks = capture.Directives.OfType<MapBlock>().ToList();

        if (sourceBlocks.Count != 1)
        {
            errors.Add("Capture must contain exactly one source block", capture.Line, capture.Column);
        }

        if (keyDirectives.Count != 1)
        {
            errors.Add("Capture must contain exactly one key directive", capture.Line, capture.Column);
        }

        if (mapBlocks.Count > 1)
        {
            errors.Add("Capture can contain at most one map block", capture.Line, capture.Column);
        }

        if (errors.HasErrors)
        {
            return errors;
        }

        var appends = capture.Directives.OfType<AppendBlock>().Select(CompileAppend).ToList();
        var nested = capture.Directives.OfType<NestedBlock>().Select(CompileNested).ToList();
        var children = capture.Directives.OfType<AST.ChildrenBlock>().Select(CompileChildren).ToList();
        var map = mapBlocks.Count == 1 ? CompileMap(mapBlocks[0]) : null;

        return new CaptureDefinition(
            CaptureId.New(),
            CompileSource(sourceBlocks[0]),
            keyDirectives[0].Property,
            map,
            appends,
            nested,
            children);
    }

    static SourceDefinition CompileSource(SourceBlock sourceBlock) => new(
        sourceBlock.SourceType,
        Api: GetValue(sourceBlock.Properties, "api"),
        Poll: GetValue(sourceBlock.Properties, "poll"),
        Auth: GetValue(sourceBlock.Properties, "auth"),
        Route: GetValue(sourceBlock.Properties, "route"),
        Path: GetValue(sourceBlock.Properties, "path"),
        Topic: GetValue(sourceBlock.Properties, "topic"));

    static MapDefinition CompileMap(MapBlock mapBlock) => new(mapBlock.Operations.Select(CompileMapOperation).ToList());

    static MapOperation CompileMapOperation(MapOperationNode operation) => operation switch
    {
        FieldRenameNode rename => new FieldRenameOperation(rename.SourceProperty, rename.TargetProperty),
        TemplateAssignNode template => new TemplateAssignOperation(template.Target, template.Template),
        TranslateNode translate => new TranslateOperation(
            translate.Target,
            translate.Source,
            translate.Entries.Select(_ => new TranslateValue(_.From, _.To)).ToList()),
        SplitNode split => new SplitOperation(split.Source, split.Separator, split.Targets),
        _ => throw new InvalidOperationException($"Unknown map operation type '{operation.GetType().Name}'")
    };

    static AppendDefinition CompileAppend(AppendBlock append) => new(
        append.EventType,
        CompileWhen(append.When),
        append.Assignments.ToDictionary(_ => _.Target, _ => _.Source));

    static WhenClause CompileWhen(WhenClauseNode when) => new(when.Type, when.Properties, when.FromValue, when.ToValue, when.Expression);

    static NestedDefinition CompileNested(NestedBlock nested) => new(
        nested.ObjectPath,
        nested.Map is not null ? CompileMap(nested.Map) : null,
        nested.Appends.Select(CompileAppend).ToList());

    static ChildrenDefinition CompileChildren(AST.ChildrenBlock children) => new(
        children.CollectionProperty,
        children.IdentifiedBy,
        children.Map is not null ? CompileMap(children.Map) : null,
        children.Appends.Select(CompileAppend).ToList());

    static string? GetValue(IReadOnlyDictionary<string, string> properties, string key) => properties.TryGetValue(key, out var value) ? value : null;
}
