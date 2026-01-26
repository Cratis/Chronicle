// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Represents a <see cref="ReplayContext"/> for a specific <see cref="ReadModelDefinition"/>.
/// </summary>
public enum GetContextError
{
    /// <summary>
    /// Represents a <see cref="ReplayContext"/> that was not found.
    /// </summary>
    NotFound = 0
}
