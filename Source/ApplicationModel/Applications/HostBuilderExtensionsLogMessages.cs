// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Holds the log messages for HostBuilderExtensions.
    /// </summary>
    public static partial class HostBuilderExtensionsLogMessages
    {
        [LoggerMessage(0, LogLevel.Information, "Setting up Aksio defaults")]
        internal static partial void SettingUpDefaults(this ILogger logger);
    }
}
