// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a read model key schema cannot be resolved for MongoDB conversion.
/// </summary>
/// <param name="readModel">The identifier of the read model that failed validation.</param>
/// <param name="reason">The specific reason why key schema validation failed.</param>
public class InvalidReadModelKeySchema(ReadModelIdentifier readModel, string reason) : Exception($"Read model '{readModel}' has an invalid key schema for MongoDB sink conversion. {reason}");

