// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a read model.
/// </summary>
/// <param name="Id">The unique identifier of the read model.</param>
/// <param name="Schema">The schema of the read model.</param>
public record ReadModel(string Id, string Schema);
