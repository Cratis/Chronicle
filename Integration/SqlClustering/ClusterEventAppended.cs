// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering;

/// <summary>
/// Records one uniquely identifiable append made while cluster membership changes.
/// </summary>
/// <param name="Id">The unique attempt identifier.</param>
[EventType]
public record ClusterEventAppended(Guid Id);
