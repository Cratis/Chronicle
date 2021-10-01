// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;

namespace Cratis.Execution
{
    /// <summary>
    /// Represents an Autofac <see cref="Module"/> for configuring dependencies related to <see cref="ExecutionContext"/>.
    /// </summary>
    public class ExecutionContextModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(_ => ExecutionContextManager.GetCurrent()).As<ExecutionContext>().InstancePerDependency();
        }
    }
}
