// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Holds log messages for <see cref="ChronicleClient"/>.
/// </summary>
internal static partial class ChronicleClientLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The software version is the default '{SoftwareVersion}'. Chronicle uses it to ignore registrations from older versions of your application during a rolling deploy, which it cannot do while every version reports the same number. Give your application a version, or set it with the 'Cratis:Chronicle:SoftwareVersion' configuration key.")]
    internal static partial void SoftwareVersionIsDefault(this ILogger logger, string softwareVersion);
}
