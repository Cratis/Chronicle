// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a read model has a container name that is empty or contains forbidden characters.
/// </summary>
/// <param name="readModel">The identifier of the read model.</param>
public class InvalidReadModelContainerName(ReadModelIdentifier readModel)
    : Exception($"The container name of read model '{readModel}' is empty or contains characters that are not allowed.");
