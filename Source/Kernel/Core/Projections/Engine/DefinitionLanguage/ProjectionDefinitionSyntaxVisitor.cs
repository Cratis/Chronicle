// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Visits a Screenplay <see cref="ProjectionSyntax"/> tree and produces the corresponding <see cref="ProjectionDefinition"/>.
/// </summary>
/// <param name="owner">The <see cref="ProjectionOwner"/> for the resulting definition.</param>
public class ProjectionDefinitionSyntaxVisitor(ProjectionOwner owner) : IProjectionSyntaxVisitor<ProjectionDefinition>
{
    bool _noAutoMap;

    /// <inheritdoc/>
    public ProjectionDefinition Visit(ProjectionSyntax syntax)
    {
        _noAutoMap = syntax.AutoMap == AutoMapMode.Disabled;

        var eventSequenceId = syntax.Sequence is null ? EventSequenceId.Log : new EventSequenceId(syntax.Sequence);
        var readModelIdentifier = syntax.ReadModel is null ? ReadModelIdentifier.Inferred : new ReadModelIdentifier(syntax.ReadModel);
        var context = ProcessBlocks(syntax.Blocks, isChildContext: false);

        return new ProjectionDefinition(
            owner,
            eventSequenceId,
            new ProjectionId(syntax.Name),
            readModelIdentifier,
            IsActive: true,
            IsRewindable: false,
            new JsonObject(),
            context.From,
            context.Join,
            context.Children,
            [],
            context.Every,
            context.RemovedWith,
            context.RemovedWithJoin,
            FromEventProperty: null,
            LastUpdated: DateTimeOffset.UtcNow,
            Tags: default,
            AutoMap: _noAutoMap ? AutoMap.Disabled : AutoMap.Enabled,
            Nested: context.Nested.Count > 0 ? context.Nested : null,
            SubscribesToAllEvents: context.SubscribesToAllEvents);
    }

    BlockContext ProcessBlocks(IEnumerable<ProjectionBlockSyntax> blocks, bool isChildContext)
    {
        var context = new BlockContext();

        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    ProcessFrom(from, context.From);
                    break;
                case EverySyntax every:
                    ProcessEvery(every, context, isChildContext);
                    break;
                case AllSyntax all:
                    context.Every = new FromEveryDefinition(ProcessMappings(all.Mappings), IncludeChildren: true)
                    {
                        AutoMap = GetAutoMapValue(all.AutoMap)
                    };
                    context.SubscribesToAllEvents = true;
                    break;
                case JoinSyntax join:
                    ProcessJoin(join, context.Join);
                    break;
                case ChildrenSyntax children:
                    ProcessChildren(children, context.Children);
                    break;
                case NestedSyntax nested:
                    ProcessNested(nested, context.Nested);
                    break;
                case RemoveWithSyntax removeWith:
                    context.RemovedWith[EventType.Parse(removeWith.Event)] = new RemovedWithDefinition(
                        removeWith.Key is not null ? ConvertKeyExpression(removeWith.Key) : PropertyExpression.NotSet,
                        removeWith.ParentKey is not null ? ConvertKeyExpression(removeWith.ParentKey) : null);
                    break;
                case RemoveViaJoinSyntax removeViaJoin:
                    context.RemovedWithJoin[EventType.Parse(removeViaJoin.Event)] = new RemovedWithJoinDefinition(
                        removeViaJoin.Key is not null ? ConvertKeyExpression(removeViaJoin.Key) : PropertyExpression.NotSet);
                    break;
                case ClearWithSyntax clearWith:
                    context.RemovedWith[EventType.Parse(clearWith.Event)] = new RemovedWithDefinition(PropertyExpression.NotSet, ParentKey: null);
                    break;
                default:
                    throw new UnsupportedProjectionSyntax(block);
            }
        }

        return context;
    }

    void ProcessFrom(FromSyntax from, Dictionary<EventType, FromDefinition> fromDefinitions)
    {
        var parentKey = from.ParentKey is not null ? ConvertKeyExpression(from.ParentKey) : null;

        foreach (var eventSpec in from.Events)
        {
            var key = eventSpec.Key is not null
                ? ConvertKeyExpression(eventSpec.Key)
                : ConvertKey(from.Key);

            fromDefinitions[EventType.Parse(eventSpec.Event)] = new FromDefinition(ProcessMappings(from.Mappings), key, parentKey);
        }
    }

    void ProcessEvery(EverySyntax every, BlockContext context, bool isChildContext)
    {
        if (isChildContext)
        {
            var properties = (Dictionary<PropertyPath, string>)context.Every.Properties;
            foreach (var (property, expression) in ProcessMappings(every.Mappings))
            {
                properties[property] = expression;
            }

            if (every.AutoMap != AutoMapMode.Inherit)
            {
                context.Every.AutoMap = GetAutoMapValue(every.AutoMap);
            }

            return;
        }

        context.Every = new FromEveryDefinition(ProcessMappings(every.Mappings), every.IncludeChildren)
        {
            AutoMap = GetAutoMapValue(every.AutoMap)
        };
    }

    void ProcessJoin(JoinSyntax join, Dictionary<EventType, JoinDefinition> joinDefinitions)
    {
        foreach (var joinEvent in join.Events)
        {
            joinDefinitions[EventType.Parse(joinEvent.Event)] = new JoinDefinition(
                new PropertyPath(join.On),
                ProcessMappings(joinEvent.Mappings),
                PropertyExpression.NotSet);
        }
    }

    void ProcessChildren(ChildrenSyntax children, Dictionary<PropertyPath, ChildrenDefinition> childrenDefinitions)
    {
        var context = ProcessBlocks(children.Blocks, isChildContext: true);

        childrenDefinitions[new PropertyPath(children.Property)] = new ChildrenDefinition(
            new PropertyPath(ConvertKeyExpression(children.IdentifiedBy).Value),
            context.From,
            context.Join,
            context.Children,
            context.Every,
            context.RemovedWith,
            context.RemovedWithJoin,
            AutoMap: GetAutoMapValue(children.AutoMap),
            Nested: context.Nested.Count > 0 ? context.Nested : null);
    }

    /// <summary>
    /// Process a nested object block and emit a <see cref="ChildrenDefinition"/> with <see cref="PropertyPath.NotSet"/>
    /// as the identifier — the engine treats the entry as scalar (one nullable child object) rather than as a collection.
    /// A <c>clear with</c> directive inside the block becomes a RemovedWith entry so the engine clears the nested
    /// object back to null when the event is observed.
    /// </summary>
    /// <param name="nested">The nested block to process.</param>
    /// <param name="nestedDefinitions">The nested dictionary on the enclosing definition.</param>
    void ProcessNested(NestedSyntax nested, Dictionary<PropertyPath, ChildrenDefinition> nestedDefinitions)
    {
        var context = ProcessBlocks(nested.Blocks, isChildContext: true);

        nestedDefinitions[new PropertyPath(nested.Property)] = new ChildrenDefinition(
            PropertyPath.NotSet,
            context.From,
            context.Join,
            context.Children,
            context.Every,
            context.RemovedWith,
            context.RemovedWithJoin,
            AutoMap: GetAutoMapValue(nested.AutoMap),
            Nested: context.Nested.Count > 0 ? context.Nested : null);
    }

    /// <summary>
    /// Converts the mappings of a block into the property expressions they are stored as.
    /// </summary>
    /// <param name="mappings">The mappings to convert.</param>
    /// <returns>The stored expression per property.</returns>
    /// <exception cref="UnsupportedProjectionSyntax">Thrown when a mapping kind has no expression to store as.</exception>
    /// <remarks>
    /// <c>clear property</c> and <c>property = null</c> are two spellings of the same act and both become
    /// <see cref="WellKnownExpressions.Null"/>. <c>clear</c> is the one the generator emits, because assigning a
    /// value and taking one away are different acts and spelling both with <c>=</c> hides that.
    /// </remarks>
    Dictionary<PropertyPath, string> ProcessMappings(IEnumerable<MappingSyntax> mappings)
    {
        var properties = new Dictionary<PropertyPath, string>();

        foreach (var mapping in mappings)
        {
            var property = new PropertyPath(mapping.Property);
            properties[property] = mapping switch
            {
                SetMappingSyntax set => ConvertSetSource(set.Source),
                ClearMappingSyntax => WellKnownExpressions.Null,
                AddMappingSyntax add => $"{WellKnownExpressions.Add}({ConvertExpressionToString(add.Value)})",
                SubtractMappingSyntax subtract => $"{WellKnownExpressions.Subtract}({ConvertExpressionToString(subtract.Value)})",
                IncrementMappingSyntax => WellKnownExpressions.Increment,
                DecrementMappingSyntax => WellKnownExpressions.Decrement,
                CountMappingSyntax => WellKnownExpressions.Count,
                _ => throw new UnsupportedProjectionSyntax(mapping)
            };
        }

        return properties;
    }

    /// <summary>
    /// Converts the source of a set mapping to the expression it is stored as.
    /// </summary>
    /// <param name="source">The source expression of the mapping.</param>
    /// <returns>The stored expression.</returns>
    /// <remarks>
    /// A null literal is the declaration language's spelling of a clear, so it becomes the clear expression rather
    /// than the empty string a null stores as elsewhere - an empty expression reads as a property path to the
    /// engine, which is not a clear and not anything else either.
    /// </remarks>
    string ConvertSetSource(ExpressionSyntax source) =>
        source is LiteralExpressionSyntax { Value: null }
            ? WellKnownExpressions.Null
            : ConvertExpressionToString(source);

    PropertyExpression ConvertKey(KeySyntax? key)
    {
        switch (key)
        {
            case null:
                return PropertyExpression.NotSet;
            case ExpressionKeySyntax expressionKey:
                return ConvertKeyExpression(expressionKey.Expression);
            case CompositeKeySyntax compositeKey:
                var parts = string.Join(", ", compositeKey.Parts.Select(part => $"{part.Property}={ConvertKeyExpression(part.Expression).Value}"));
                return new PropertyExpression($"{WellKnownExpressions.Composite}({compositeKey.Type}, {parts})");
            default:
                throw new UnsupportedProjectionSyntax(key);
        }
    }

    PropertyExpression ConvertKeyExpression(ExpressionSyntax expression) =>
        expression switch
        {
            LiteralExpressionSyntax { Value: string value } => new PropertyExpression($"{WellKnownExpressions.Value}({value})"),
            _ => new PropertyExpression(ConvertExpressionToString(expression))
        };

    string ConvertExpressionToString(ExpressionSyntax expression) =>
        expression switch
        {
            PathExpressionSyntax path => path.Path,
            EventContextExpressionSyntax eventContext => $"{WellKnownExpressions.EventContext}({eventContext.Path})",
            EventSourceIdExpressionSyntax => WellKnownExpressions.EventSourceId,
            CausedByExpressionSyntax causedBy => causedBy.Property is null
                ? WellKnownExpressions.CausedBy
                : $"{WellKnownExpressions.CausedBy}({causedBy.Property})",
            LiteralExpressionSyntax literal => FormatLiteralForStorage(literal.Value),
            TemplateExpressionSyntax template => ConvertTemplateToString(template),
            RawExpressionSyntax raw => raw.Text,
            _ => throw new UnsupportedProjectionSyntax(expression)
        };

    string ConvertTemplateToString(TemplateExpressionSyntax template)
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

        return $"`{builder}`";
    }

    string FormatLiteralForStorage(object? value) =>
        value switch
        {
            null => string.Empty, // Null is stored as an empty string
            string text => $"\"{text}\"", // Strings keep their quotes to distinguish them from property names
            bool boolean => boolean.ToString(), // Stored as "True"/"False"
            double number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    AutoMap GetAutoMapValue(AutoMapMode mode) =>
        mode switch
        {
            AutoMapMode.Enabled => AutoMap.Enabled,
            AutoMapMode.Disabled => AutoMap.Disabled,

            // Inherit resolves to the projection-level default: disabled when 'no automap' is declared, enabled otherwise
            _ => _noAutoMap ? AutoMap.Disabled : AutoMap.Enabled
        };

    sealed class BlockContext
    {
        public Dictionary<EventType, FromDefinition> From { get; } = [];
        public Dictionary<EventType, JoinDefinition> Join { get; } = [];
        public Dictionary<PropertyPath, ChildrenDefinition> Children { get; } = [];
        public Dictionary<PropertyPath, ChildrenDefinition> Nested { get; } = [];
        public Dictionary<EventType, RemovedWithDefinition> RemovedWith { get; } = [];
        public Dictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin { get; } = [];
        public FromEveryDefinition Every { get; set; } = new(new Dictionary<PropertyPath, string>(), IncludeChildren: false);
        public bool SubscribesToAllEvents { get; set; }
    }
}
