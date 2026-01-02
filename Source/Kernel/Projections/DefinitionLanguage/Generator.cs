// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var sb = new StringBuilder();
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title;

        // Projection declaration - use read model name since the original projection name is not stored in ProjectionDefinition
        sb.AppendLine($"projection {readModelName}Projection => {readModelName}");

        // FromEvery block
        if (definition.FromEvery.Properties.Count > 0)
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

        return sb.ToString();
    }

    static string Indent(int level) => string.Concat(Enumerable.Repeat(Tab, level));

    void GenerateEveryBlock(StringBuilder sb, FromEveryDefinition every, int indent)
    {
        sb.AppendLine($"{Indent(indent)}every");

        // AutoMap directive
        if (every.AutoMap == AutoMap.Enabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}automap");
        }

        foreach (var kv in every.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }

        if (!every.IncludeChildren)
        {
            sb.AppendLine($"{Indent(indent + 1)}exclude children");
        }
    }

    void GenerateOnEventBlock(StringBuilder sb, string eventTypeName, FromDefinition from, int indent)
    {
        sb.Append($"{Indent(indent)}from {eventTypeName}");

        // Inline key if it's simple
        if (from.Key.IsSet())
        {
            sb.AppendLine($" key {from.Key.Value}");
        }
        else
        {
            sb.AppendLine();
        }

        // Parent key if present
        if (from.ParentKey?.IsSet() == true)
        {
            sb.AppendLine($"{Indent(indent + 1)}parent key {from.ParentKey.Value}");
        }

        // AutoMap directive
        if (from.AutoMap == AutoMap.Enabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}automap");
        }

        // Property mappings
        foreach (var kv in from.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }
    }

    void GenerateJoinBlock(StringBuilder sb, string eventTypeName, JoinDefinition join, int indent)
    {
        // Note: The join name (e.g., "Group") is not stored in JoinDefinition, so we use the event type name
        // This means we can't perfectly reconstruct the original DSL if multiple events were in one join block
        sb.AppendLine($"{Indent(indent)}join {eventTypeName} on {join.On.Path}");
        sb.AppendLine($"{Indent(indent + 1)}events {eventTypeName}");

        // Inline key if present (though join typically doesn't have a key)
        if (join.Key.IsSet())
        {
            sb.AppendLine($"{Indent(indent + 1)}key {join.Key.Value}");
        }

        // AutoMap directive
        if (join.AutoMap == AutoMap.Enabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}automap");
        }

        foreach (var kv in join.Properties)
        {
            GeneratePropertyMapping(sb, kv.Key, kv.Value, indent + 1);
        }
    }

    void GenerateChildrenBlock(StringBuilder sb, PropertyPath collectionName, ChildrenDefinition children, int indent)
    {
        sb.AppendLine($"{Indent(indent)}children {collectionName.Path}")
            .AppendLine($"{Indent(indent + 1)}identified by {children.IdentifiedBy.Path}");

        // AutoMap directive
        if (children.AutoMap == AutoMap.Enabled)
        {
            sb.AppendLine($"{Indent(indent + 1)}automap");
        }

        // FromEvery for children
        if (children.All.Properties.Count > 0)
        {
            GenerateEveryBlock(sb, children.All, indent + 1);
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
            sb.AppendLine($"{Indent(indent + 1)}parent key {removedWith.ParentKey.Value}");
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
        if (expression.StartsWith("$eventContext(") && expression.EndsWith(")"))
        {
            var property = expression[14..^1];
            return $"$eventContext.{property}";
        }

        // Check if it's a simple property reference (no operators, no literals)
        // If it doesn't contain special characters and isn't a literal, prefix with e.
        if (!expression.Contains("$") && 
            !expression.Contains("`") && 
            !expression.StartsWith("\"") && 
            !expression.StartsWith("'") &&
            !expression.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !expression.Equals("false", StringComparison.OrdinalIgnoreCase) &&
            !expression.Equals("null", StringComparison.OrdinalIgnoreCase) &&
            !double.TryParse(expression, out _) &&
            !expression.Contains("."))
        {
            return $"e.{expression}";
        }

        return expression;
    }
}
