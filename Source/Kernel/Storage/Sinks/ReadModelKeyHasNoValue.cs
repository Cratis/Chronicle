// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a sink is asked to address a read model instance by a key that has no value.
/// </summary>
/// <param name="readModel">The identifier of the read model the key was for.</param>
public class ReadModelKeyHasNoValue(ReadModelIdentifier readModel) : Exception($"The key for read model '{readModel}' had no value, so the instance cannot be addressed. Check that the key expression of the projection resolves to a value for every event - an unresolved key would otherwise write every instance under the same key.");
