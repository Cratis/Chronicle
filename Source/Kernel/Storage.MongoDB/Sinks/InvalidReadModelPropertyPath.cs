// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a property path does not exist in a read model schema during MongoDB conversion.
/// </summary>
/// <param name="readModel">The identifier of the read model that failed validation.</param>
/// <param name="propertyPath">The property path that could not be resolved in the schema.</param>
public class InvalidReadModelPropertyPath(ReadModelIdentifier readModel, PropertyPath propertyPath) : Exception($"Read model '{readModel}' does not contain schema information for property path '{propertyPath}'.");

