// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Projections;
using Cratis.Strings;

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
    public static RuleId GetRuleId(this Type type)
    {
        return type.FullName ?? $"{type.Namespace}.{type.Name}";
    }
}
