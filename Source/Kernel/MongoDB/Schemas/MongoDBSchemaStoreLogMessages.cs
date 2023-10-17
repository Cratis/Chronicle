// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.MongoDB.Schemas;

internal static partial class MongoDBSchemaStoreLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Populating event schemas for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void Populating(this ILogger<MongoDBSchemaStore> logger, MicroserviceId microserviceId, TenantId tenantId);

    [LoggerMessage(2, LogLevel.Information, "Registering event schema for '{Name}' (Id: {EventType} - Generation: {Generation}) for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void Registering(this ILogger<MongoDBSchemaStore> logger, string name, EventTypeId eventType, EventGeneration generation, MicroserviceId microserviceId, TenantId tenantId);
}
