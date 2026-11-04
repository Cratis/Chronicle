// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

/// <summary>
/// An immediate projection with a session-specific key.
/// </summary>
public class a_session_immediate_projection : an_immediate_projection
{
    protected override ProjectionSessionId? SessionId => new(Guid.NewGuid());
}
