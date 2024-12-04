// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents the error of performing a <see cref="IJobStep"/>.
/// </summary>
/// <param name="ErrorMessages">The error messages.</param>
/// <param name="ExceptionStackTrace">The optional exception stack trace.</param>
public record PerformJobStepError(IEnumerable<string> ErrorMessages, string? ExceptionStackTrace);