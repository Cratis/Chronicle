// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class CompatibilityValidatorLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed to validate compatibility between client and server")]
    internal static partial void FailedToValidateCompatibility(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "{Details}")]
    internal static partial void IncompatibleWithServer(this ILogger<ChronicleConnection> logger, string details);

    [LoggerMessage(LogLevel.Information, "Compatibility check passed - client {ClientVersion} speaking protocol {ProtocolVersion} against server {ServerVersion} speaking protocol {ServerProtocolVersion}")]
    internal static partial void CompatibilityCheckPassed(this ILogger<ChronicleConnection> logger, string clientVersion, string protocolVersion, string serverVersion, string serverProtocolVersion);

    [LoggerMessage(LogLevel.Warning, "Could not ask the server to check compatibility: {Message}")]
    internal static partial void FailedToCheckCompatibility(this ILogger<ChronicleConnection> logger, string message);

    [LoggerMessage(LogLevel.Warning, "Failed to retrieve server descriptor set: {Message}")]
    internal static partial void FailedToRetrieveServerDescriptorSet(this ILogger<ChronicleConnection> logger, string message);
}
