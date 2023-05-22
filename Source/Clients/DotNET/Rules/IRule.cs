// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Defines the minimum of a business rule.
/// </summary>
public interface IRule
{
    /// <summary>
    /// Gets the unique identifier of the rule.
    /// </summary>
    RuleId Identifier { get; }
}
