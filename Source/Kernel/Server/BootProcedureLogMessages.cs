// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Server;

internal static partial class BootProcedureLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Priming caches for all event sequences for all microservices and tenants")]
    internal static partial void PrimingEventSequenceCaches(this ILogger logger);

    [LoggerMessage(1, LogLevel.Information, "Populating schema store")]
    internal static partial void PopulateSchemaStore(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Populating identity store")]
    internal static partial void PopulateIdentityStore(this ILogger logger);

    [LoggerMessage(3, LogLevel.Information, "Rehydrating projections")]
    internal static partial void RehydrateProjections(this ILogger logger);
}
