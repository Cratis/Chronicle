// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Defines the minimum of a business rule.
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Gets the unique identifier of the business rule.
    /// </summary>
    BusinessRuleId Identifier { get; }
}
