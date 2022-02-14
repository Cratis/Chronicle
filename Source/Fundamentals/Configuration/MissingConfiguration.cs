// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Exception that gets thrown when a specific configuration is missing.
    /// </summary>
    public class MissingConfiguration : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingConfiguration"/> class.
        /// </summary>
        /// <param name="type">Type of configuration object.</param>
        /// <param name="file">File it was expecting to find.</param>
        public MissingConfiguration(Type type, string file) : base($"Missing configuration file '${file}' for '${type.FullName}'")
        {
        }
    }
}
