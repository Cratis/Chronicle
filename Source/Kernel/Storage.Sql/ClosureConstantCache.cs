// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Shared storage for closure constants between the evaluatable filter and parameter evaluator.
/// </summary>
internal static class ClosureConstantCache
{
    static readonly ConcurrentDictionary<string, ConstantExpression> _closures = new();

    /// <summary>
    /// Store a closure constant expression.
    /// </summary>
    /// <param name="closureConstant">The closure constant to store.</param>
    public static void Store(ConstantExpression closureConstant)
    {
        var key = closureConstant.Value?.GetType().FullName ?? closureConstant.Value?.GetType().Name ?? "unknown";
        _closures[key] = closureConstant;
    }

    /// <summary>
    /// Get a closure constant expression by type name.
    /// </summary>
    /// <param name="key">The type name key.</param>
    /// <returns>The closure constant, or null if not found.</returns>
    public static ConstantExpression? Get(string key)
    {
        _closures.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Clear all stored closures.
    /// </summary>
    public static void Clear()
    {
        _closures.Clear();
    }
}
