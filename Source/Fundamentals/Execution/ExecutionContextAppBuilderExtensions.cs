// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> for the purpose of working with <see cref="ExecutionContext"/>.
/// </summary>
public static class ExecutionContextAppBuilderExtensions
{
    const string TenantIdHeader = "Tenant-ID";

    /// <summary>
    /// Use execution context for an application.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to use <see cref="ExecutionContext"/> for.</param>
    /// <returns><see cref="IApplicationBuilder"/> for builder continuation.</returns>
    /// <remarks>
    /// This adds a middleware that will look for an HTTP header to use as Tenant identifier and establishes
    /// the correct <see cref="ExecutionContext"/> according to this.
    /// </remarks>
    public static IApplicationBuilder UseExecutionContext(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var tenantId = TenantId.Development;

            if (context.Request.Headers.ContainsKey(TenantIdHeader))
            {
                tenantId = context.Request.Headers[TenantIdHeader][0];
            }

            var executionContextManager = app.ApplicationServices.GetService(typeof(IExecutionContextManager)) as IExecutionContextManager;
            executionContextManager!.Establish(tenantId, Guid.NewGuid().ToString());
            await next.Invoke().ConfigureAwait(false);
        });

        return app;
    }
}
