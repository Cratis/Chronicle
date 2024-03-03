// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Boot;

internal static partial class BootProceduresLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Performing boot procedures")]
    internal static partial void PerformingBootProcedures(this ILogger<BootProcedures> logger);
}
