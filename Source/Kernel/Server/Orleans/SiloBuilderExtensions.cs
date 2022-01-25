// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Extensions.Orleans.Execution;

namespace Orleans.Hosting
{
    /// <summary>
    /// Extension methods for extending the <see cref="ISiloBuilder"/>.
    /// </summary>
    public static class SiloBuilderExtensions
    {
        /// <summary>
        /// Add support for <see cref="ExecutionContext"/> for grain calls.
        /// </summary>
        /// <param name="builder"><see cref="ISiloBuilder"/> to add to.</param>
        /// <returns><see cref="ISiloBuilder"/> for builder continuation.</returns>
        public static ISiloBuilder AddExecutionContext(this ISiloBuilder builder)
        {
            builder.AddOutgoingGrainCallFilter<ExecutionContextOutgoingCallFilter>();
            builder.AddIncomingGrainCallFilter<ExecutionContextIncomingCallFilter>();
            return builder;
        }
    }
}
