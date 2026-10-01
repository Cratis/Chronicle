// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a sink cannot safely create an isolated replay target.
/// </summary>
/// <param name="sinkType">The unsupported sink type.</param>
public class ReplayIsolationNotSupported(SinkTypeId sinkType) : Exception($"Sink '{sinkType}' does not support isolated reducer replays");
