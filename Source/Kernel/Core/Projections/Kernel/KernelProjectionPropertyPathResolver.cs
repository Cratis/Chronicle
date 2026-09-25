// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Resolves a <see cref="PropertyPath"/> from a read model property accessor.
/// </summary>
/// <remarks>
/// The kernel cannot reach the client's expression extensions, and a system projection needs the same thing they
/// provide: the path a lambda names, extracted at definition time rather than executed. Anything that is not a
/// member-access chain rooted in the lambda parameter is rejected outright - a definition that silently resolved to
/// an empty path would register a projection that writes nowhere.
/// </remarks>
public static class KernelProjectionPropertyPathResolver
{
    /// <summary>
    /// Resolve the <see cref="PropertyPath"/> a read model property accessor names.
    /// </summary>
    /// <typeparam name="TReadModel">Type of the read model.</typeparam>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">The accessor to resolve.</param>
    /// <returns>The resolved <see cref="PropertyPath"/>.</returns>
    /// <exception cref="InvalidKernelProjectionPropertyAccessor">Thrown when the accessor is not a member-access chain rooted in the lambda parameter.</exception>
    public static PropertyPath Resolve<TReadModel, TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor)
    {
        var current = propertyAccessor.Body;

        if (current is UnaryExpression unary)
        {
            current = unary.Operand;
        }

        var members = new List<string>();
        while (current is MemberExpression member)
        {
            members.Insert(0, member.Member.Name);
            current = member.Expression;
        }

        if (members.Count == 0 || current is not ParameterExpression)
        {
            throw new InvalidKernelProjectionPropertyAccessor(typeof(TReadModel), propertyAccessor.ToString());
        }

        return new PropertyPath(string.Join('.', members));
    }
}
