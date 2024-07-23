// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Extension methods for working with reducer types.
/// </summary>
public static class RuleTypeExtensions
{
    /// <summary>
    /// Get the rule id for a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="RuleId"/> for the type.</returns>
    public static RuleId GetRuleId(this Type type) =>
        type.FullName ?? $"{type.Namespace}.{type.Name}";

    /// <summary>
    /// Get the rule id for a type.
    /// </summary>
    /// <param name="rule"><see cref="IRule"/> to get from.</param>
    /// <returns>The <see cref="RuleId"/> for the type.</returns>
    public static RuleId GetRuleId(this IRule rule)
    {
        var type = rule.GetType();
        return type.GetRuleId();
    }
}
