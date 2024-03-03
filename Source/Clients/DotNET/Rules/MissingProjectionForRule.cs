// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Rules;

/// <summary>
/// Exception that gets thrown when there is no projection for a specific rule.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingProjectionForRule"/> class.
/// </remarks>
/// <param name="ruleId"><see cref="RuleId"/> that there is no projection for.</param>
public class MissingProjectionForRule(RuleId ruleId) : Exception($"There is no projection for rule with id '{ruleId.Value}'. This indicates that the rule is not stateful and does not have a projection.")
{
}
