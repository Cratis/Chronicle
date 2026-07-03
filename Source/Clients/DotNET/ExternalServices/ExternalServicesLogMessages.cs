// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ExternalServices;

internal static partial class ExternalServicesLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Registering external service '{Name}'")]
    internal static partial void RegisterExternalService(this ILogger<ExternalServices> logger, string name);
}
