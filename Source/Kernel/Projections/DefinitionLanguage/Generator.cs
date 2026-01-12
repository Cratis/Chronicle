// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Generates a projection DSL string from a <see cref="ProjectionDefinition"/> using the new indentation-based syntax.
/// </summary>
public class Generator : IGenerator
{
    const string Tab = "    ";
    static readonly string[] _compositeSeparator = [", "];

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var sb = new StringBuilder();
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title;

        // Projection declaration - use read model name since the original projection name is not stored in ProjectionDefinition
        sb.AppendLine($"projection {readModelName}Projection => {readModelName}");

        // Sequence directive - only output if it's not the default (Log)
        if (definition.EventSequenceId != EventSequenceId.Log)
        {
            sb.AppendLine($"{Indent(1)}sequence {definition.EventSequenceId.Value}");
        }

        // NoAutoMap directive at projection level - output if AutoMap is explicitly disabled
        if (definition.AutoMap == AutoMap.Disabled)
        {
            sb.AppendLine($"{Indent(1)}no automap");
        }

        // FromEvery block - only output if it has meaningful content
        // Don't output if it only has 'exclude children' directive without any other content
        var hasEveryContent = definition.FromEvery.Properties.Count > 0 || definition.FromEvery.AutoMap == AutoMap.Disabled;
        var hasNoOtherBlocks = definition.From.Count == 0 && definition.Join.Count == 0 && definition.Children.Count == 0;
        if (hasEveryContent || hasNoOtherBlocks)
        {
            GenerateEveryBlock(sb, definition.FromEvery, 1);
        }

        // On event blocks
        foreach (var kv in definition.From)
        {
            GenerateOnEventBlock(sb, kv.Key.Id.Value, kv.Value, 1);
        }

        // Join blocks - need to group by OnProperty to reconstruct original join blocks
        foreach (var group in definition.Join.GroupBy(kv => kv.Value.On))
        {
            var joins = group.ToList();

            GenerateJoinBlock(sb, joins[0].Key.Id.Value, group.Key.Path, joins, 1);
        }

        // Children blocks
        foreach (var kv in definition.Children)
        {
            GenerateChildrenBlock(sb, kv.Key, kv.Value, definition.AutoMap, 1);
        }

        // RemovedWith blocks
        foreach (var kv in definition.RemovedWith)
        {
            GenerateRemovedWithBlock(sb, kv.Key.Id.Value, kv.Value, 1);
        }

        // RemovedWithJoin blocks
        foreach (var kv in definition.RemovedWithJoin)
        {
            GenerateRemovedWithJoinBlock(sb, kv.Key.Id.Value, kv.Value, 1);
        }

        var result = sb.ToString();
        return result.EndsWith('\n') ? result : result + '\n';
    }

    static string Indent(int level) => string.Concat(Enumerable.Repeat(Tab, level));

    static string EscapeIfKeyword(string identifier)
    {
        var lowerIdentifier = identifier.ToLowerInvariant();
        return Keywords.All.Contains(lowerIdentifier) ? $"@{identifier}" : identifier;
    }

    static string EscapePropertyPath(string path)
    {
        var segments = path.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = EscapeIfKeyword(segments[i]);
        }
        return string.Join('.', segments);
    }

    void GenerateEveryBlock(StringBuilder sb, FromEveryDefinition every, int indent, bool isChildContext = false)
    {
        sb.AppendLine($"{Indent(indent)}every");

        // NoAutoMap directive - only output if disabled (since enabled is now the default)
        if (every.AutoMap == AutoMap.Disabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}no automap");
        }

        foreach (var kv in every.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }

        // Only output 'exclude children' for top-level every blocks, not for child every blocks
        if (!isChildContext && !every.IncludeChildren)
        {
            sb.AppendLine($"{Indent(indent + 1)}exclude children");
        }
    }

    void GenerateOnEventBlock(StringBuilder sb, string eventTypeName, FromDefinition from, int indent)
    {
        // Determine if we should use inline key syntax or block key syntax
        // Use inline syntax for simple keys
        // Use block syntax for composite keys or when other content exists
        var hasCompositeKey = from.Key.IsSet() && from.Key.Value.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal) && from.Key.Value.EndsWith(')');
        var isDefaultKey = from.Key.IsSet() && from.Key.Value == WellKnownExpressions.EventSourceId;
        var useInlineKey = from.Key.IsSet() && !hasCompositeKey && !isDefaultKey;

        // Build from statement with optional inline key (skip if it's the default $eventSourceId)
        if (useInlineKey)
        {
            sb.AppendLine($"{Indent(indent)}from {eventTypeName} key {from.Key.Value}");
        }
        else
        {
            sb.AppendLine($"{Indent(indent)}from {eventTypeName}");
        }

        // Composite key directive (always in block)
        if (hasCompositeKey)
        {
            var keyValue = from.Key.Value;

            // Parse composite key: $composite(TypeName, CustomerId=customerId, OrderNumber=orderNumber)
            var innerContent = keyValue[(WellKnownExpressions.Composite.Length + 1)..^1];
            var parts = innerContent.Split(_compositeSeparator, StringSplitOptions.None);

            // Extract type name from first part
            var typeName = parts.Length > 0 ? parts[0].Trim() : "CompositeKey";

            sb.AppendLine($"{Indent(indent + 1)}key {typeName}");

            // Skip the first part (type name) and process the property mappings
            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                var equalsIndex = part.IndexOf('=');
                if (equalsIndex > 0)
                {
                    var propertyName = EscapePropertyPath(part.Substring(0, equalsIndex));
                    var expression = part.Substring(equalsIndex + 1);
                    sb.AppendLine($"{Indent(indent + 2)}{propertyName} = {ConvertExpressionForOutput(expression)}");
                }
            }
        }

        // Parent key if present (skip if it's the default $eventSourceId)
        if (from.ParentKey?.IsSet() == true && from.ParentKey.Value != WellKnownExpressions.EventSourceId)
        {
            sb.AppendLine($"{Indent(indent + 1)}parent {ConvertExpressionForOutput(from.ParentKey.Value)}");
        }

        // Property mappings - filter out redundant mappings when they would be handled by automap
        // Only output explicit mappings where the target property name differs from the source expression
        foreach (var kv in from.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }
    }

    void GenerateJoinBlock(StringBuilder sb, string joinName, string onProperty, List<KeyValuePair<EventType, JoinDefinition>> joins, int indent)
    {
        sb.AppendLine($"{Indent(indent)}join {joinName} on {onProperty}");

        foreach (var kv in joins)
        {
            var eventType = kv.Key.Id.Value;
            var join = kv.Value;

            sb.AppendLine($"{Indent(indent + 1)}with {eventType}");

            // Property mappings - filter out redundant mappings when automap is enabled
            foreach (var prop in join.Properties)
            {
                GeneratePropertyMapping(sb, prop.Key, prop.Value, indent + 2);
            }
        }
    }

    void GenerateChildrenBlock(StringBuilder sb, PropertyPath collectionName, ChildrenDefinition children, AutoMap parentAutoMap, int indent)
    {
        // Determine the effective AutoMap for this children block
        // If the children block has AutoMap set to Inherit, use the parent's setting
        // Otherwise use the children's own AutoMap setting
        var effectiveAutoMap = children.AutoMap == AutoMap.Inherit ? parentAutoMap : children.AutoMap;

        sb.AppendLine($"{Indent(indent)}children {collectionName.Path} identified by {children.IdentifiedBy.Path}");

        // NoAutoMap directive - only output if disabled and different from parent
        if (effectiveAutoMap == AutoMap.Disabled && parentAutoMap != AutoMap.Disabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}no automap");
        }
        else if (effectiveAutoMap == AutoMap.Enabled && parentAutoMap != AutoMap.Enabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}automap");
        }

        // FromEvery for children
        if (children.All.Properties.Count > 0)
        {
            GenerateEveryBlock(sb, children.All, indent + 1, isChildContext: true);
        }

        // Child on event blocks
        foreach (var kv in children.From)
        {
            GenerateOnEventBlock(sb, kv.Key.Id.Value, kv.Value, indent + 1);
        }

        // Child join blocks - need to group by OnProperty to reconstruct original join blocks
        foreach (var group in children.Join.GroupBy(kv => kv.Value.On))
        {
            var joins = group.ToList();

            GenerateJoinBlock(sb, joins[0].Key.Id.Value, group.Key.Path, joins, indent + 1);
        }

        // Nested children
        foreach (var kv in children.Children)
        {
            GenerateChildrenBlock(sb, kv.Key, kv.Value, effectiveAutoMap, indent + 1);
        }

        // RemovedWith blocks
        foreach (var kv in children.RemovedWith)
        {
            GenerateRemovedWithBlock(sb, kv.Key.Id.Value, kv.Value, indent + 1);
        }

        // RemovedWithJoin blocks
        foreach (var kv in children.RemovedWithJoin)
        {
            GenerateRemovedWithJoinBlock(sb, kv.Key.Id.Value, kv.Value, indent + 1);
        }
    }

    void GenerateRemovedWithBlock(StringBuilder sb, string eventTypeName, RemovedWithDefinition removedWith, int indent)
    {
        sb.Append($"{Indent(indent)}remove with {eventTypeName}");

        // Inline key if present
        if (removedWith.Key.IsSet())
        {
            sb.AppendLine($" key {removedWith.Key.Value}");
        }
        else
        {
            sb.AppendLine();
        }

        if (removedWith.ParentKey?.IsSet() == true)
        {
            sb.AppendLine($"{Indent(indent + 1)}parent {ConvertExpressionForOutput(removedWith.ParentKey.Value)}");
        }
    }

    void GenerateRemovedWithJoinBlock(StringBuilder sb, string eventTypeName, RemovedWithJoinDefinition removedWithJoin, int indent)
    {
        sb.Append($"{Indent(indent)}remove via join on {eventTypeName}");

        if (removedWithJoin.Key.IsSet())
        {
            sb.AppendLine($" key {removedWithJoin.Key.Value}");
        }
        else
        {
            sb.AppendLine();
        }
    }

    void GeneratePropertyMapping(StringBuilder sb, PropertyPath property, string expression, int indent)
    {
        // Skip 'id' or 'Id' property as it's implicitly set
        if (property.Path.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var propertyName = EscapePropertyPath(property.Path);

        var normalizedExpression = NormalizeExpression(expression);

        // Check for arithmetic operations from C# projections ($add, $subtract)
        if (normalizedExpression.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
        {
            var innerExpression = normalizedExpression[(WellKnownExpressions.Add.Length + 1)..^1];
            sb.AppendLine($"{Indent(indent)}add {propertyName} by {ConvertExpressionForOutput(innerExpression)}");
            return;
        }

        if (normalizedExpression.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
        {
            var innerExpression = normalizedExpression[(WellKnownExpressions.Subtract.Length + 1)..^1];
            sb.AppendLine($"{Indent(indent)}subtract {propertyName} by {ConvertExpressionForOutput(innerExpression)}");
            return;
        }

        // Parse the expression to determine the operation type from DSL
        if (normalizedExpression == WellKnownExpressions.Increment)
        {
            sb.AppendLine($"{Indent(indent)}increment {propertyName}");
        }
        else if (normalizedExpression == WellKnownExpressions.Decrement)
        {
            sb.AppendLine($"{Indent(indent)}decrement {propertyName}");
        }
        else if (normalizedExpression == WellKnownExpressions.Count)
        {
            sb.AppendLine($"{Indent(indent)}count {propertyName}");
        }
        else
        {
            sb.AppendLine($"{Indent(indent)}{propertyName} = {ConvertExpressionForOutput(normalizedExpression)}");
        }
    }

    string ConvertExpressionForOutput(string expression)
    {
        var normalizedExpression = NormalizeExpression(expression);

        // Convert $add(expression) wrapper - this comes from C# projections
        if (normalizedExpression.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
        {
            var innerExpression = normalizedExpression[(WellKnownExpressions.Add.Length + 1)..^1];
            return ConvertExpressionForOutput(innerExpression);
        }

        // Convert $subtract(expression) wrapper - this comes from C# projections
        if (normalizedExpression.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
        {
            var innerExpression = normalizedExpression[(WellKnownExpressions.Subtract.Length + 1)..^1];
            return ConvertExpressionForOutput(innerExpression);
        }

        // Convert $eventContext(property) to $eventContext.property
        if (normalizedExpression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
        {
            var property = normalizedExpression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $"{WellKnownExpressions.EventContext}.{property}";
        }

        // Convert $causedBy(property) to $causedBy.property
        if (normalizedExpression.StartsWith($"{WellKnownExpressions.CausedBy}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
        {
            var property = normalizedExpression[10..^1];
            return $"{WellKnownExpressions.CausedBy}.{property}";
        }

        // Handle plain $causedBy
        if (normalizedExpression.Equals(WellKnownExpressions.CausedBy, StringComparison.Ordinal))
        {
            return WellKnownExpressions.CausedBy;
        }

        // Convert C# boolean ToString() to DSL format
        if (normalizedExpression.Equals("True", StringComparison.Ordinal))
        {
            return "true";
        }
        if (normalizedExpression.Equals("False", StringComparison.Ordinal))
        {
            return "false";
        }

        // Convert empty string (C# null representation) to DSL null
        if (string.IsNullOrEmpty(normalizedExpression))
        {
            return "null";
        }

        // String literals already have quotes from Compiler - return as-is
        if (normalizedExpression.StartsWith('\"') && normalizedExpression.EndsWith('\"'))
        {
            return normalizedExpression;
        }

        // Numeric literals
        if (double.TryParse(normalizedExpression, out _))
        {
            return normalizedExpression;
        }

        // Event context, template expressions, etc.
        if (normalizedExpression.Contains('$') || normalizedExpression.Contains('`'))
        {
            return normalizedExpression;
        }

        // Property paths and simple names - check if they're keywords and escape if needed
        var parts = normalizedExpression.Split(['.']);
        var escapedParts = parts.Select(part => Keywords.All.Contains(part.ToLowerInvariant()) ? $"@{part}" : part).ToArray();
        return string.Join('.', escapedParts);
    }

    string NormalizeExpression(string expression)
    {
        if (expression.StartsWith("+=", StringComparison.Ordinal))
        {
            return $"{WellKnownExpressions.Add}({expression[2..].Trim()})";
        }

        if (expression.StartsWith("-=", StringComparison.Ordinal))
        {
            return $"{WellKnownExpressions.Subtract}({expression[2..].Trim()})";
        }

        if (expression.Equals("increment", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Increment;
        }

        if (expression.Equals("decrement", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Decrement;
        }

        if (expression.Equals("count", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Count;
        }

        return expression;
    }
}
