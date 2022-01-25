// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Execution
{
    /// <summary>
    /// Represents helpers for getting details about the Runtime environment.
    /// </summary>
    public static class RuntimeEnvironment
    {
        /// <summary>
        /// Gets whether or not we're running in development or not.
        /// </summary>
        public static readonly bool IsDevelopment;

        static RuntimeEnvironment()
        {
            IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }
    }
}
