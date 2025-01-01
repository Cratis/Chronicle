// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a projection.
/// </summary>
/// <param name="Id">Identifier of the projection.</param>
/// <param name="IsActive">Whether or not it is an active projection.</param>
/// <param name="ModelName">Name of the model used.</param>
public record Projection(Guid Id, bool IsActive, string ModelName);
