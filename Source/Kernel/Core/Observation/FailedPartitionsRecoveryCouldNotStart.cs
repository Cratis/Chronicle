// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an operator request starts no failed partition recovery.
/// </summary>
/// <param name="observer">The observer whose failed partitions could not be retried.</param>
public class FailedPartitionsRecoveryCouldNotStart(ObserverKey observer) : Exception($"Could not start or resume recovery for any failed partition on observer '{observer}'.");
