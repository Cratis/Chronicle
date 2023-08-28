// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules;

/// <summary>
/// Exception that gets thrown when there is no projection for a specific rule.
/// </summary>
public class MissingProjectionForRule : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingProjectionForRule"/> class.
    /// </summary>
    /// <param name="ruleId"><see cref="RuleId"/> that there is no projection for.</param>
    public MissingProjectionForRule(RuleId ruleId)
        : base($"There is no projection for rule with id '{ruleId.Value}'. This indicates that the rule is not stateful and does not have a projection.")
    {
    }
}
