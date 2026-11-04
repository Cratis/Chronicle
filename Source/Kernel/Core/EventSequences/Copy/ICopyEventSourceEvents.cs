// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Defines a durable job that copies the events of one event source to another event source in the same event sequence.
/// </summary>
public interface ICopyEventSourceEvents : IJob<CopyEventSourceEventsRequest>;
