// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Orleans.Transactions;

/// <summary>
/// Holds constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The key for the correlation id in the <see cref="RequestContext"/>.
    /// </summary>
    public const string CorrelationIdKey = "CorrelationId";

    /// <summary>
    /// The key for the unit of work results in the <see cref="RequestContext"/>.
    /// </summary>
    public const string UnitOfWorkResultsKey = "UnitOfWorkResults";
}
