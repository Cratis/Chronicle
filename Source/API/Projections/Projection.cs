// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Models;
using Cratis.Chronicle.Projections;

namespace Cratis.API.Projections;

/// <summary>
/// Represents a projection.
/// </summary>
/// <param name="Id">Identifier of the projection.</param>
/// <param name="Name">Name of the projection.</param>
/// <param name="IsActive">Whether or not it is an active projection.</param>
/// <param name="ModelName">Name of the model used.</param>
public record Projection(ProjectionId Id, ProjectionName Name, bool IsActive, ModelName ModelName);
