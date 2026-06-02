// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a job for reindexing changed constraint indexes.
/// </summary>
public interface IReindexConstraints : IJob<ReindexConstraintsRequest>;
