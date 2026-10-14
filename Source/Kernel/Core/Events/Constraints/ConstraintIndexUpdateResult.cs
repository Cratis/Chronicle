// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Reports an individual updater without hiding another updater's failure.
/// </summary>
/// <param name="Updater">The updater that was attempted.</param>
/// <param name="Outcome">How the attempt ended.</param>
/// <param name="Error">The error, when the outcome was not applied.</param>
public record ConstraintIndexUpdateResult(IUpdateConstraintIndex Updater, ConstraintIndexUpdateOutcome Outcome, Exception? Error);
