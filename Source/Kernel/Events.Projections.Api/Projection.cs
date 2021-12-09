// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Api
{
    public record Projection(Guid Id, string Name, string State, string Positions);
}
