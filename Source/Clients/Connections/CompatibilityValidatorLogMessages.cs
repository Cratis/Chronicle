// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class CompatibilityValidatorLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed to validate compatibility between client and server")]
    internal static partial void FailedToValidateCompatibility(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Client is incompatible with server. Errors: {Errors}")]
    internal static partial void IncompatibleWithServer(this ILogger<ChronicleConnection> logger, string errors);

    [LoggerMessage(LogLevel.Information, "Client compatibility check passed")]
    internal static partial void CompatibilityCheckPassed(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to retrieve server descriptor set: {Message}")]
    internal static partial void FailedToRetrieveServerDescriptorSet(this ILogger<ChronicleConnection> logger, string message);
}
