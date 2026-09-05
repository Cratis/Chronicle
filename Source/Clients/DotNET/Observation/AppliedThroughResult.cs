// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the result of checking whether a named set of observers have applied through a target position.
/// </summary>
/// <param name="IsSuccess">Whether every requested observer reached <see cref="AppliedThroughOutcome.Ready"/>.</param>
/// <param name="Results">The typed outcome for every requested observer.</param>
public record AppliedThroughResult(bool IsSuccess, IEnumerable<AppliedThroughObserverResult> Results);
