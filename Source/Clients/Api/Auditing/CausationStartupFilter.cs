// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Auditing;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Chronicle.Api.Auditing;

/// <summary>
/// Represents a startup filter for causation.
/// </summary>
public class CausationStartupFilter : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<CausationMiddleware>();
            next(app);
        };
    }
}
