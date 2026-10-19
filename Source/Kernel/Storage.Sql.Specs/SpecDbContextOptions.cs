// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Extensions for building <see cref="DbContextOptions"/> in the specs.
/// </summary>
public static class SpecDbContextOptions
{
    /// <summary>
    /// Disables EF Core's internal service provider cache for the options being built.
    /// </summary>
    /// <remarks>
    /// The specs build far more than twenty distinct option sets in one process, and EF Core throws
    /// <c>ManyServiceProvidersCreatedWarning</c> for any created after the twentieth, which makes the
    /// failing spec depend on test order. Every spec harness goes through this helper.
    /// </remarks>
    /// <typeparam name="TBuilder">The type of options builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder for continuation.</returns>
    public static TBuilder WithoutServiceProviderCaching<TBuilder>(this TBuilder builder)
        where TBuilder : DbContextOptionsBuilder
    {
        builder.EnableServiceProviderCaching(false);
        return builder;
    }
}
