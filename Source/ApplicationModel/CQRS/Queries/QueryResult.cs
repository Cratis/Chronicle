// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Queries
{
    /// <summary>
    /// Represents the result coming from performing a query.
    /// </summary>
    /// <param name="Data">Data returned by the query.</param>
    /// <param name="IsSuccess">Whether or not everything is Ok.</param>
    public record QueryResult(object Data, bool IsSuccess);
}
