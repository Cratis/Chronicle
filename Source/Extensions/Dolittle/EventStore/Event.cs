// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Extensions.Dolittle.EventStore
{
    public record Event(uint Id, ExecutionContext ExecutionContext, EventMetadata Metadata, Aggregate Aggregate, EventHorizon EventHorizon, BsonDocument Content);
}
