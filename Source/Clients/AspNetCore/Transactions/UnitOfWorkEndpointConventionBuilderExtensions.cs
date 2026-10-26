// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Transactions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for completing the request unit of work before a minimal API result is written.
/// </summary>
public static class UnitOfWorkEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Claims and commits an unclaimed request unit of work before returning the endpoint result,
    /// replacing it with problem details on failure. Exceptions roll back and are rethrown.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// Requires the request unit-of-work middleware. This opt-in does not depend on
    /// <see cref="ChronicleUnitOfWorkOptions.ReportFailuresBeforeResponseStarts"/> and never completes an existing claim.
    /// </remarks>
    public static TBuilder WithUnitOfWork<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(new UnitOfWorkEndpointFilter());
}
