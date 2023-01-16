// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

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
        var executionContextManager = app.ApplicationServices.GetRequiredService<IExecutionContextManager>();
        executionContextManager.Establish(ExecutionContextManager.GlobalMicroserviceId);
        app.Use(async (context, next) =>
        {
            var tenantId = TenantId.Development;

            if (context.Request.Headers.ContainsKey(TenantIdHeader))
            {
                tenantId = context.Request.Headers[TenantIdHeader][0];
            }
            executionContextManager.Establish(tenantId, CorrelationId.New());

            try
            {
                await next.Invoke();
            }
            catch (InvalidOperationException)
            {
                // TODO: We're catching this exception to avoid WebSockets that are terminated. It tries to continue on middlewares
                // and one of these is doing modifications on headers and we get 'Headers are read-only, response has already started'.
                // The reason fir this is that the original request that was upgraded to WebSockets continues when the controller action
                // is done.
            }
        });

        return app;
    }
}
