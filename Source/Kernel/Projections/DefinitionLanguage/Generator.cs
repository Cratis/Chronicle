// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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

        // Join blocks
        foreach (var kv in definition.Join)
        {
            GenerateJoinBlock(sb, kv.Key.Id.Value, kv.Value, 1);
        }

        // Children blocks
        foreach (var kv in definition.Children)
        {
            GenerateChildrenBlock(sb, kv.Key, kv.Value, 1);
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
        // Use inline syntax for simple keys when there are no other inline options (like automap)
        // Use block syntax for composite keys or when other content exists
        var hasCompositeKey = from.Key.IsSet() && from.Key.Value.StartsWith("$composite(") && from.Key.Value.EndsWith(')');
        var useInlineKey = from.Key.IsSet() && !hasCompositeKey;

        // Build from statement with optional inline key
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
            var innerContent = keyValue.Substring("$composite(".Length, keyValue.Length - "$composite(".Length - 1);
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
                    var propertyName = part.Substring(0, equalsIndex);
                    var expression = part.Substring(equalsIndex + 1);
                    sb.AppendLine($"{Indent(indent + 2)}{propertyName} = {ConvertExpressionForOutput(expression)}");
                }
            }
        }

        // Parent key if present
        if (from.ParentKey?.IsSet() == true)
        {
            sb.AppendLine($"{Indent(indent + 1)}parent {ConvertExpressionForOutput(from.ParentKey.Value)}");
        }

        // NoAutoMap directive - only output if disabled (since enabled is now the default)
        if (from.AutoMap == AutoMap.Disabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}no automap");
        }

        // Property mappings
        foreach (var kv in from.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }
    }

    void GenerateJoinBlock(StringBuilder sb, string joinName, JoinDefinition join, int indent)
    {
        // Note: joinName is the EventType key (which holds the join name for root-level joins)
        sb.AppendLine($"{Indent(indent)}join {joinName} on {join.On.Path}");

        // For now, we can't perfectly reconstruct which event types were in the join,
        // so we'll need to look at other metadata or make assumptions
        // For simplicity, assume single event type matching the join name for now
        // TODO: Store event types explicitly in JoinDefinition if needed
        sb.AppendLine($"{Indent(indent + 1)}events {joinName}");

        // Inline key if present (though join typically doesn't have a key)
        if (join.Key.IsSet())
        {
            sb.AppendLine($"{Indent(indent + 1)}key {join.Key.Value}");
        }

        // NoAutoMap directive - only output if disabled (since enabled is now the default)
        if (join.AutoMap == AutoMap.Disabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}no automap");
        }

        foreach (var kv in join.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }
    }

    void GenerateChildrenBlock(StringBuilder sb, PropertyPath collectionName, ChildrenDefinition children, int indent)
    {
        sb.AppendLine($"{Indent(indent)}children {collectionName.Path} id {children.IdentifiedBy.Path}");

        // NoAutoMap directive - only output if disabled (since enabled is now the default)
        if (children.AutoMap == AutoMap.Disabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}no automap");
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

        // Child join blocks
        foreach (var kv in children.Join)
        {
            GenerateJoinBlock(sb, kv.Key.Id.Value, kv.Value, indent + 1);
        }

        // Nested children
        foreach (var kv in children.Children)
        {
            GenerateChildrenBlock(sb, kv.Key, kv.Value, indent + 1);
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

        // Parse the expression to determine the operation type
        if (expression.StartsWith("+="))
        {
            var value = expression[2..].Trim();
            sb.AppendLine($"{Indent(indent)}add {property.Path} by {ConvertExpressionForOutput(value)}");
        }
        else if (expression.StartsWith("-="))
        {
            var value = expression[2..].Trim();
            sb.AppendLine($"{Indent(indent)}subtract {property.Path} by {ConvertExpressionForOutput(value)}");
        }
        else if (expression == "increment")
        {
            sb.AppendLine($"{Indent(indent)}increment {property.Path}");
        }
        else if (expression == "decrement")
        {
            sb.AppendLine($"{Indent(indent)}decrement {property.Path}");
        }
        else if (expression == "count")
        {
            sb.AppendLine($"{Indent(indent)}count {property.Path}");
        }
        else
        {
            // Simple assignment
            sb.AppendLine($"{Indent(indent)}{property.Path} = {ConvertExpressionForOutput(expression)}");
        }
    }

    string ConvertExpressionForOutput(string expression)
    {
        // Convert $eventContext(property) to $eventContext.property
        if (expression.StartsWith("$eventContext(") && expression.EndsWith(')'))
        {
            var property = expression[14..^1];
            return $"$eventContext.{property}";
        }

        // Convert $causedBy(property) to $causedBy.property
        if (expression.StartsWith("$causedBy(") && expression.EndsWith(')'))
        {
            var property = expression[10..^1];
            return $"$causedBy.{property}";
        }

        // Handle plain $causedBy
        if (expression.Equals("$causedBy", StringComparison.Ordinal))
        {
            return "$causedBy";
        }

        // Convert C# boolean ToString() to DSL format
        if (expression.Equals("True", StringComparison.Ordinal))
        {
            return "true";
        }
        if (expression.Equals("False", StringComparison.Ordinal))
        {
            return "false";
        }

        // Convert empty string (C# null representation) to DSL null
        if (string.IsNullOrEmpty(expression))
        {
            return "null";
        }

        // String literals already have quotes from Compiler - return as-is
        if (expression.StartsWith('\"') && expression.EndsWith('\"'))
        {
            return expression;
        }

        // Numeric literals
        if (double.TryParse(expression, out _))
        {
            return expression;
        }

        // Event context, template expressions, etc.
        if (expression.Contains('$') || expression.Contains('`'))
        {
            return expression;
        }

        // Everything else (property paths, simple names) - return as-is
        return expression;
    }
}
