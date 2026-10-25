// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a failed partition recovery job cannot start or resume.
/// </summary>
/// <param name="partition">The partition whose recovery could not start.</param>
/// <param name="observer">The observer whose recovery could not start.</param>
public class FailedPartitionRecoveryCouldNotStart(Key partition, ObserverKey observer) : Exception($"Could not start or resume recovery for failed partition '{partition}' on observer '{observer}'.");
