// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Extension methods for <see cref="Expression"/>.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Get <see cref="PropertyPath"/> from an <see cref="Expression"/>.
    /// </summary>
    /// <param name="expression"><see cref="Expression"/> to get from.</param>
    /// <returns>The full <see cref="PropertyPath"/>.</returns>
    public static PropertyPath GetPropertyPath(this Expression expression)
    {
        if (expression is LambdaExpression lambda)
        {
            var current = lambda.Body;
            var members = new List<string>();
            if (current is UnaryExpression unary)
            {
                current = unary.Operand;
            }
            while (current is MemberExpression memberExpression)
            {
                current = memberExpression.Expression;
                members.Insert(0, memberExpression.Member.Name);
            }
            return new PropertyPath(string.Join('.', members));
        }

        return new PropertyPath(string.Empty);
    }

    /// <summary>
    /// Try to get a <see cref="PropertyPath"/> from an <see cref="Expression"/>, requiring it to be a
    /// member-access accessor rooted in the lambda parameter (for example <c language="csharp">e => e.Property</c> or <c language="csharp">e => e.Parent.Child</c>).
    /// </summary>
    /// <param name="expression"><see cref="Expression"/> to get from.</param>
    /// <param name="propertyPath">When this method returns <see langword="true"/>, the extracted <see cref="PropertyPath"/>; otherwise <see cref="PropertyPath.NotSet"/>.</param>
    /// <returns>True if the expression is a supported member-access accessor, false otherwise.</returns>
    /// <remarks>
    /// Unlike <see cref="GetPropertyPath(Expression)"/> — which silently returns an empty or partial path for
    /// unsupported shapes — this method rejects anything that is not a pure member-access chain bottoming out at the
    /// lambda parameter. Method calls, string interpolation, arithmetic, conditionals, constants, and expressions that
    /// ignore the parameter (such as <c language="csharp">_ => DateTimeOffset.UtcNow</c>) all return <see langword="false"/>, because the builder
    /// extracts a property path from the expression at definition time rather than executing it.
    /// <para>
    /// The one deliberate exception is a call to a well-known <see cref="DerivedPropertyFunctions">derived property
    /// function</see> — such as <c language="csharp">c => c.Occurred.ISOWeek()</c> — as the outermost call in the
    /// expression, on a receiver that is itself a supported member-access chain. That is folded into the returned
    /// <see cref="PropertyPath"/> rather than rejected; any other method call, including a call to an unrecognized
    /// zero-argument extension method, is still rejected exactly as before.
    /// </para>
    /// </remarks>
    public static bool TryGetPropertyPath(this Expression expression, out PropertyPath propertyPath)
    {
        propertyPath = PropertyPath.NotSet;

        if (expression is not LambdaExpression lambda)
        {
            return false;
        }

        var current = lambda.Body;
        if (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            current = unary.Operand;
        }

        var members = new List<string>();

        if (current is MethodCallExpression methodCall && TryGetDerivedPropertyFunctionReceiver(methodCall, out var receiver))
        {
            members.Insert(0, methodCall.Method.Name);
            current = receiver;
            if (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } receiverUnary)
            {
                current = receiverUnary.Operand;
            }
        }

        while (current is MemberExpression memberExpression)
        {
            members.Insert(0, memberExpression.Member.Name);
            current = memberExpression.Expression;
        }

        if (members.Count == 0 || current is not ParameterExpression)
        {
            return false;
        }

        propertyPath = new PropertyPath(string.Join('.', members));
        return true;
    }

    /// <summary>
    /// Determine if a method call is a recognized, zero-argument <see cref="DerivedPropertyFunctions">derived
    /// property function</see> invoked as an extension method (for example <c language="csharp">dateTimeOffset.ISOWeek()</c>),
    /// as opposed to an arbitrary method call - which is still rejected.
    /// </summary>
    /// <param name="methodCall">The <see cref="MethodCallExpression"/> to check.</param>
    /// <param name="receiver">When this method returns <see langword="true"/>, the expression the function was called on.</param>
    /// <returns>True if the method call is a recognized derived property function, false otherwise.</returns>
    static bool TryGetDerivedPropertyFunctionReceiver(MethodCallExpression methodCall, out Expression receiver)
    {
        receiver = null!;

        if (methodCall.Arguments.Count != 1 ||
            !methodCall.Method.IsStatic ||
            !methodCall.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false) ||
            !DerivedPropertyFunctions.All.ContainsKey(methodCall.Method.Name))
        {
            return false;
        }

        receiver = methodCall.Arguments[0];
        return true;
    }
}
