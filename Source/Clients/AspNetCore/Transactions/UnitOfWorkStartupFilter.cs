// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents a <see cref="IStartupFilter"/> for units of work.
/// </summary>
public class UnitOfWorkStartupFilter : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            if (app.ApplicationServices.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value.UseAutomaticMiddleware)
            {
                app.UseMiddleware<UnitOfWorkMiddleware>();
            }
            next(app);
        };
    }
}
