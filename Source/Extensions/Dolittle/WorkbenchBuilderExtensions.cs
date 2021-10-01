// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.AspNetCore.Workbench;

namespace Cratis.Extensions.Dolittle
{
    /// <summary>
    /// Extension methods for working with the <see cref="WorkbenchBuilder"/>.
    /// </summary>
    public static class WorkbenchBuilderExtensions
    {
        /// <summary>
        /// Use the Dolittle API surface for the workbench.
        /// </summary>
        /// <param name="builder"><see cref="WorkbenchBuilder"/> to use it for.</param>
        /// <returns><see cref="WorkbenchBuilder"/> for continuation.</returns>
        public static WorkbenchBuilder UseDolittle(this WorkbenchBuilder builder)
        {
            builder.UseAPIFrom(typeof(WorkbenchBuilderExtensions).Assembly);
            return builder;
        }
    }
}
