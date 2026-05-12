// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// The exception that is thrown when a reducer read model definition is invalid for registration.
/// </summary>
/// <param name="readModel">The read model identifier.</param>
/// <param name="reason">The reason why the definition is invalid.</param>
public class InvalidReadModelDefinitionForReducer(ReadModelIdentifier readModel, string reason) : Exception($"Read model '{readModel}' has an invalid definition for reducer registration. {reason}");
