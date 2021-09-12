// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
namespace Orleans
{
    /// <summary>
    /// Extension methods for extending the <see cref="IClientBuilder"/>.
    /// </summary>
    public static class OrleansClientBuilderExtensions
    {
        /// <summary>
        /// Use <see cref="ExecutionContext"/> for all outgoing grain calls.
        /// </summary>
        /// <param name="builder"><see cref="IClientBuilder"/> to use it for.</param>
        /// <returns><see cref="IClientBuilder"/> for builder continuation.</returns>
        public static IClientBuilder UseExecutionContext(this IClientBuilder builder)
        {
            builder.AddOutgoingGrainCallFilter<ExecutionContextOutgoingCallFilter>();
            return builder;
        }
    }
}
