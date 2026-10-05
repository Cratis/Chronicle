// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Reducers.when_replaying;

/// <summary>
/// The read model a replayed reducer builds.
/// </summary>
/// <param name="Total">The weighted total of every number seen.</param>
public record ReplayedTotal(int Total);
