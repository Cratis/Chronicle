// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Reads whether a resume was refused by the admission rather than for any other reason.
/// </summary>
public static class ResumeResults
{
    /// <summary>
    /// Gets whether the resume was refused admission.
    /// </summary>
    /// <param name="result">The result of the resume.</param>
    /// <returns>True if the admission refused it.</returns>
    public static bool WasNotAdmitted(this Result<ResumeJobSuccess, ResumeJobError> result) =>
        result.TryGetError(out var error) && error.IsT0 && error.AsT0 == CannotResumeJobError.NotAdmitted;
}
