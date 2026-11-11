// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Provides non-recording dependencies for production hot-path benchmarks. Proxy dispatch overhead is
/// identical on main and the candidate; it must not be interpreted as production sink latency.
/// </summary>
public class BenchmarkNoOpProxy : DispatchProxy
{
    Func<string, object?> _answer = null!;

    /// <summary>
    /// Creates a dependency that answers calls without recording their history.
    /// </summary>
    /// <typeparam name="T">The dependency interface.</typeparam>
    /// <param name="answer">The result to return for each member name.</param>
    /// <returns>The no-op dependency.</returns>
    internal static T Bind<T>(Func<string, object?> answer)
        where T : class
    {
        var instance = Create<T, BenchmarkNoOpProxy>();
        ((BenchmarkNoOpProxy)(object)instance)._answer = answer;
        return instance;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _answer(targetMethod!.Name);
}
