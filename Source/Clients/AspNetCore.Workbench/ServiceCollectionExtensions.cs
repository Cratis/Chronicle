// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.AspNetCore.Workbench;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Use Cratis workbench.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
        /// <param name="workbenchBuilderCallback">Optional <see cref="Action{WorkbenchBuilder}">Callback</see> for building configuration for the workbench.</param>
        /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
        public static IServiceCollection AddCratisWorkbench(this IServiceCollection services, Action<WorkbenchBuilder>? workbenchBuilderCallback = null)
        {
            var mvcBuilder = services.AddControllers();
            if (workbenchBuilderCallback != default)
            {
                var workbenchBuilder = new WorkbenchBuilder();
                workbenchBuilderCallback(workbenchBuilder);
                if (workbenchBuilder.APIAssembly != default)
                {
                    mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(workbenchBuilder.APIAssembly));
                }
            }

            mvcBuilder.AddJsonOptions(_ => _.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
            return services;
        }
    }
}
