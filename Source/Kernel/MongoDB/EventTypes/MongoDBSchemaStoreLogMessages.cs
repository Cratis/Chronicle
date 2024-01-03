// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.MongoDB.EventTypes;

internal static partial class MongoDBSchemaStoreLogMessages
{
    [LoggerMessage(1, LogLevel.Debug, "Populating event schemas for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void Populating(this ILogger<EventTypesStorage> logger, MicroserviceId microserviceId, TenantId tenantId);

    [LoggerMessage(2, LogLevel.Debug, "Registering event schema for '{Name}' (Id: {EventType} - Generation: {Generation}) for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void Registering(this ILogger<EventTypesStorage> logger, string name, EventTypeId eventType, EventGeneration generation, MicroserviceId microserviceId, TenantId tenantId);
}
