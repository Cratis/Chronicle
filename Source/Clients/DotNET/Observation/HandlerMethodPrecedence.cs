// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Orders candidate handler methods so that the one that should win dispatch for an event type comes first.
/// </summary>
/// <remarks>
/// Reactors and reducers discover their handlers by convention, from the first parameter type. That means a
/// helper extracted for readability - which naturally takes the same event as its first parameter - competes
/// with the real handler for the same event type. Reflection makes no promise about the order
/// <see cref="Type.GetMethods()"/> returns them in, so without an explicit precedence the helper can win and
/// silently take the handler's place, with nothing thrown and no side effect ever happening.
/// </remarks>
internal static class HandlerMethodPrecedence
{
    /// <summary>
    /// Order candidate handler methods with the one that should win dispatch first.
    /// </summary>
    /// <param name="methods">The candidate <see cref="MethodInfo">methods</see> to order.</param>
    /// <returns>The methods ordered by descending precedence.</returns>
    /// <remarks>
    /// A public method beats a non-public one, because the documented convention is that handlers are public
    /// and anything else is an implementation detail. Between two methods of the same accessibility the richest
    /// signature wins - the one taking the most parameters asked for the most context, so it is the more
    /// specific handler. The final ordering by name breaks a genuine tie the same way on every run rather than
    /// leaving the outcome to reflection order.
    /// </remarks>
    public static IEnumerable<MethodInfo> ByPrecedence(this IEnumerable<MethodInfo> methods) =>
        methods
            .OrderByDescending(_ => _.IsPublic)
            .ThenByDescending(_ => _.GetParameters().Length)
            .ThenBy(_ => _.Name, StringComparer.Ordinal);
}
