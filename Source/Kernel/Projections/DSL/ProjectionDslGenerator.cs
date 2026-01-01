// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Generates a projection DSL string from a <see cref="ProjectionDefinition"/>.
/// </summary>
/// <remarks>
/// This is the legacy pipe-based DSL generator. A new generator for the rules-based DSL is pending implementation.
/// </remarks>
[Obsolete("This generator creates legacy pipe-based DSL. A new generator for the rules-based DSL will be implemented.")]
public class ProjectionDslGenerator : IProjectionDslGenerator
{
    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var sb = new StringBuilder();

        // Read model declaration
        sb.AppendLine(readModelDefinition.GetSchemaForLatestGeneration().Title);

        // FromEvery / event context mappings first
        foreach (var kv in definition.FromEvery.Properties)
        {
            var prop = kv.Key.Path;
            var expr = kv.Value;
            sb.AppendLine($"| {prop}={expr}");
        }

        // Key statements (per event where a key expression exists)
        foreach (var kv in definition.From)
        {
            var ev = kv.Key;
            var from = kv.Value;
            if (from.Key.IsSet())
            {
                var keyExpr = from.Key.Expression;

                // If the key expression looks like Event.Property (contains a dot and not a special expression)
                if (!string.IsNullOrWhiteSpace(keyExpr) && !keyExpr.StartsWith("$"))
                {
                    // assume "Event.Property"
                    sb.AppendLine($"| key={ev.Id.Value}.{keyExpr}");
                }
                else
                {
                    sb.AppendLine($"| key={keyExpr}");
                }
            }
        }

        // Property mappings and operations from specific events
        foreach (var kv in definition.From)
        {
            var ev = kv.Key;
            var from = kv.Value;
            var evName = ev.Id.Value;

            foreach (var propKv in from.Properties)
            {
                var prop = propKv.Key.Path;
                var expr = propKv.Value;

                if (!string.IsNullOrEmpty(expr) && expr.StartsWith("\"") && expr.EndsWith("\""))
                {
                    // constant
                    var inner = expr.Trim('"');
                    sb.AppendLine($"| {prop}=\"{inner}\" on {evName}");
                }
                else if (!string.IsNullOrEmpty(expr) && expr.StartsWith("$add("))
                {
                    var inner = expr[5..^1];
                    sb.AppendLine($"| {prop}+{evName}.{inner}");
                }
                else if (!string.IsNullOrEmpty(expr) && expr.StartsWith("$subtract("))
                {
                    var inner = expr[10..^1];
                    sb.AppendLine($"| {prop}-{evName}.{inner}");
                }
                else if (expr == "$increment")
                {
                    sb.AppendLine($"| {prop} increment by {evName}");
                }
                else if (expr == "$decrement")
                {
                    sb.AppendLine($"| {prop} decrement by {evName}");
                }
                else if (expr == "$count")
                {
                    sb.AppendLine($"| {prop} count {evName}");
                }
                else if (!string.IsNullOrEmpty(expr) && expr.StartsWith("$eventContext."))
                {
                    sb.AppendLine($"| {prop}={evName}.{$"{expr}"}");
                }
                else
                {
                    // default mapping: property=Event.property
                    // if expr is empty or equals prop, use prop name
                    var eventProp = string.IsNullOrEmpty(expr) ? prop : expr;

                    // If a join-on is present (key used as join property), emit join token
                    if (from.Key.IsSet() && !string.IsNullOrWhiteSpace(from.Key.Expression) && !from.Key.Expression.StartsWith("$"))
                    {
                        sb.AppendLine($"| {prop}={evName}.{eventProp} join {from.Key.Expression}");
                    }
                    else
                    {
                        sb.AppendLine($"| {prop}={evName}.{eventProp}");
                    }
                }
            }
        }

        // Join definitions (explicit Join dictionary)
        foreach (var kv in definition.Join)
        {
            var ev = kv.Key;
            var join = kv.Value;
            var evName = ev.Id.Value;

            foreach (var propKv in join.Properties)
            {
                var prop = propKv.Key.Path;
                var eventProp = propKv.Value;
                sb.AppendLine($"| {prop}={evName}.{eventProp} join {join.On.Path}");
            }
        }

        // Children
        foreach (var kv in definition.Children)
        {
            var childName = kv.Key.Path;
            var child = kv.Value;

            sb.AppendLine($"| {childName}=[");

            // identifier (if set)
            if (child.IdentifiedBy.IsSet)
            {
                sb.AppendLine($"|    {child.IdentifiedBy.Path} identifier");
            }

            // child FromEvery mappings
            foreach (var c in child.All.Properties)
            {
                sb.AppendLine($"|    {c.Key.Path}={c.Value}");
            }

            // child key statements
            foreach (var f in child.From)
            {
                if (f.Value.Key.IsSet())
                {
                    var keyExpr = f.Value.Key.Expression;
                    if (!string.IsNullOrWhiteSpace(keyExpr) && !keyExpr.StartsWith("$"))
                    {
                        sb.AppendLine($"|    key={f.Key.Id.Value}.{keyExpr}");
                    }
                    else
                    {
                        sb.AppendLine($"|    key={keyExpr}");
                    }
                }
            }

            // child properties
            foreach (var f in child.From)
            {
                var ev = f.Key;
                var evName = ev.Id.Value;
                foreach (var p in f.Value.Properties)
                {
                    var prop = p.Key.Path;
                    var expr = p.Value;
                    if (!string.IsNullOrEmpty(expr) && expr.StartsWith("\"") && expr.EndsWith("\""))
                    {
                        var inner = expr.Trim('"');
                        sb.AppendLine($"|    {prop}=\"{inner}\" on {evName}");
                    }
                    else if (!string.IsNullOrEmpty(expr) && expr.StartsWith("$add("))
                    {
                        var inner = expr[5..^1];
                        sb.AppendLine($"|    {prop}+{evName}.{inner}");
                    }
                    else if (!string.IsNullOrEmpty(expr) && expr.StartsWith("$subtract("))
                    {
                        var inner = expr[10..^1];
                        sb.AppendLine($"|    {prop}-{evName}.{inner}");
                    }
                    else if (expr == "$increment")
                    {
                        sb.AppendLine($"|    {prop} increment by {evName}");
                    }
                    else if (expr == "$decrement")
                    {
                        sb.AppendLine($"|    {prop} decrement by {evName}");
                    }
                    else if (expr == "$count")
                    {
                        sb.AppendLine($"|    {prop} count {evName}");
                    }
                    else if (!string.IsNullOrEmpty(expr) && expr.StartsWith("$eventContext."))
                    {
                        sb.AppendLine($"|    {prop}={expr}");
                    }
                    else
                    {
                        var eventProp = string.IsNullOrEmpty(expr) ? prop : expr;
                        if (f.Value.Key.IsSet() && !string.IsNullOrWhiteSpace(f.Value.Key.Expression) && !f.Value.Key.Expression.StartsWith("$"))
                        {
                            sb.AppendLine($"|    {prop}={evName}.{eventProp} join {f.Value.Key.Expression}");
                        }
                        else
                        {
                            sb.AppendLine($"|    {prop}={evName}.{eventProp}");
                        }
                    }
                }
            }

            // child removedWith
            foreach (var r in child.RemovedWith)
            {
                sb.AppendLine($"|    removedWith {r.Key.Id.Value}");
            }

            sb.AppendLine("| ]");
        }

        // removedWith at top-level
        foreach (var kv in definition.RemovedWith)
        {
            sb.AppendLine($"| removedWith {kv.Key.Id.Value}");
        }

        return sb.ToString();
    }
}
