// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.Operator;

/// <summary>
/// Exception thrown when an explicit constraint reindex cannot be started.
/// </summary>
/// <param name="reason">The reason the reindex was refused or failed to start.</param>
public class ConstraintReindexCouldNotBeStarted(string reason) : Exception(reason);
