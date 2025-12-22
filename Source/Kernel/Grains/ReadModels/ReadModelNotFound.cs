// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// The exception that is thrown when a read model is not found.
/// </summary>
/// <param name="identifier">The read model identifier that was not found.</param>
public class ReadModelNotFound(ReadModelId identifier) : Exception($"Read model with identifier '{identifier}' was not found");
