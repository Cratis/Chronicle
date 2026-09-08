// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Reads whether a query can produce nothing.
/// </summary>
/// <remarks>
/// A query declared as returning <c>Task&lt;Order?&gt;</c> says the order may not exist. Dropping that from the
/// contract does not just lose documentation - it makes the generated implementation map an absent read model as
/// though it were present, which is a null reference at the first property read. Both generators consult this so
/// the contract and the code that fills it agree on whether nothing is a possible answer.
/// </remarks>
public static class QueryNullability
{
    static readonly NullabilityInfoContext _context = new();

    static readonly string[] _wrappers =
    [
        "Task`1",
        "ValueTask`1",
        "IObservable`1",
        "ISubject`1",
        "BehaviorSubject`1",
        "ReplaySubject`1",
        "Subject`1"
    ];

    /// <summary>
    /// Determines whether the value a query produces can be absent.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <returns>True when the produced value is nullable.</returns>
    public static bool ResultIsNullable(MethodInfo method)
    {
        try
        {
            var info = _context.Create(method.ReturnParameter);
            while (info.GenericTypeArguments.Length == 1 && _wrappers.Contains(info.Type.Name, StringComparer.Ordinal))
            {
                info = info.GenericTypeArguments[0];
            }

            return info.Type.IsValueType
                ? Nullable.GetUnderlyingType(info.Type) is not null
                : info.ReadState == NullabilityState.Nullable;
        }
        catch (Exception ex)
        {
            // Nullability metadata references types the isolated context may not resolve. Treating the result as
            // always present matches what the contracts declared before nullability was read at all.
            Console.WriteLine($"  WARNING: Could not read the nullability of '{method.Name}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Determines whether a query produces its value synchronously.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <returns>True when the method does not return an awaitable.</returns>
    public static bool IsSynchronous(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(Task))
        {
            return false;
        }

        return !returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>);
    }
}
