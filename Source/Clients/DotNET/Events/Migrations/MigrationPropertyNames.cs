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
    internal static PropertyName Resolve(LambdaExpression expression, JsonNamingPolicy? namingPolicy) =>
        new(ResolvePath(expression.Body, namingPolicy));

    /// <summary>
    /// Resolves a member chain, where a <c language="csharp">Select</c> over a collection addresses every element of it.
    /// </summary>
    /// <param name="body">The body of the accessor, or of the element selector.</param>
    /// <param name="namingPolicy">The event payload's JSON naming policy.</param>
    /// <returns>The serialized property path, with <c language="csharp">[]</c> after a collection whose every element is addressed.</returns>
    static string ResolvePath(Expression body, JsonNamingPolicy? namingPolicy)
    {
        var current = body;
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

        if (current is MethodCallExpression { Arguments: [var collection, var selector] } call &&
            call.Method.Name == nameof(Enumerable.Select) &&
            (selector is UnaryExpression { Operand: LambdaExpression quoted } ? quoted : selector as LambdaExpression) is { } elementSelector)
        {
            return $"{ResolvePath(collection, namingPolicy)}[].{ResolvePath(elementSelector.Body, namingPolicy)}";
        }

        return string.Join('.', members);
    }
}
