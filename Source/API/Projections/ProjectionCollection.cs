// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Projections;

/// <summary>
/// Represents information about projection collections.
/// </summary>
/// <param name="Name">Name of projection.</param>
/// <param name="DocumentCount">Count of documents.</param>
public record ProjectionCollection(string Name, int DocumentCount);
