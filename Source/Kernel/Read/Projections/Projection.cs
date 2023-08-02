// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Read.Projections;

public record Projection(ProjectionId Id, ProjectionName Name, bool IsActive, ModelName ModelName);
