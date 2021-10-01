// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.AspNetCore.Workbench
{
    /// <summary>
    /// Defines the builder for the workbench.
    /// </summary>
    public class WorkbenchBuilder
    {
        /// <summary>
        /// Gets the <see cref="Assembly"/> to use for the API.
        /// </summary>
        public Assembly? APIAssembly { get; private set; }

        /// <summary>
        /// Use the API from the given <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> to use.</param>
        public WorkbenchBuilder UseAPIFrom(Assembly assembly)
        {
            APIAssembly = assembly;
            return this;
        }
    }
}
