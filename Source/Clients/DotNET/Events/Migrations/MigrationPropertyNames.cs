// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Resolves typed migration paths at the boundary between member accessors and raw JSON paths.
/// </summary>
internal static class MigrationPropertyNames
{
    /// <summary>
    /// Resolves an accessor using the builder's serialization context when it provides one.
    /// </summary>
    /// <param name="builder">The receiving property builder.</param>
    /// <param name="expression">The typed member accessor.</param>
    /// <returns>The resolved JSON path, or the original CLR path for custom builders without a resolver.</returns>
    internal static PropertyName Resolve(IEventMigrationPropertyBuilder builder, LambdaExpression expression) =>
        builder is IResolveMigrationPropertyNames resolver
            ? resolver.ResolvePropertyName(expression)
            : new PropertyName(expression.GetPropertyPath());

    /// <summary>
    /// Resolves member names with the same attribute precedence as JSON serialization.
    /// </summary>
    /// <param name="expression">The typed member accessor.</param>
    /// <param name="namingPolicy">The event payload's JSON naming policy.</param>
    /// <returns>The serialized property path.</returns>
    internal static PropertyName Resolve(LambdaExpression expression, JsonNamingPolicy? namingPolicy)
    {
        var current = expression.Body;
        if (current is UnaryExpression unary)
        {
            current = unary.Operand;
        }

        var members = new Stack<string>();
        while (current is MemberExpression member)
        {
            // Expression trees can identify the base declaration of a virtual property. Resolve the
            // override on the receiver type before reading the member's own serialization attributes.
            var serializedMember = member.Member;
            if (member.Member is PropertyInfo { GetMethod.IsVirtual: true } property && member.Expression is { } receiver)
            {
                serializedMember = receiver.Type.GetProperties()
                    .FirstOrDefault(candidate => candidate.GetMethod?.GetBaseDefinition() == property.GetMethod.GetBaseDefinition()) ?? property;
            }

            // System.Text.Json does not inherit JsonPropertyName from an overridden base declaration.
            var name = serializedMember.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: false)?.Name
                ?? namingPolicy?.ConvertName(serializedMember.Name)
                ?? serializedMember.Name;
            members.Push(name);
            current = member.Expression;
        }

        return new PropertyName(string.Join('.', members));
    }
}
