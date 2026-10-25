// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>Tracks entries seeded by one optional prepared page.</summary>
internal interface IProjectionPageCache
{
    /// <summary>Begins tracking reads seeded by a new prepared page.</summary>
    void BeginPreparedPage();

    /// <summary>Removes only entries seeded by the prepared page.</summary>
    void ClearPreparedPage();
}
