// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// The exception that is thrown when delivery commit invariants fail before I/O; nothing is written.
/// </summary>
/// <param name="reason">The violated invariant.</param>
public sealed class InvalidDeliveryCommit(string reason) : Exception($"Invalid observer delivery commit: {reason}");
