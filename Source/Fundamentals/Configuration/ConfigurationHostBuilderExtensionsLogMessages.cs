// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Log messages for <see cref="ConfigurationHostBuilderExtensions"/>.
    /// </summary>
    public static partial class ConfigurationHostBuilderExtensionsLogMessages
    {
        [LoggerMessage(0, LogLevel.Information, "Building configuration for '{Type}' for file '{File}'")]
        internal static partial void BuildingConfigurationFor(this ILogger logger, Type type, string file);
    }
}
