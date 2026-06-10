// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents a failure from an individual append operation.
/// </summary>
/// <param name="ConstraintViolations">Collection of constraint violations.</param>
/// <param name="HasConcurrencyViolation">Whether a concurrency violation occurred.</param>
/// <param name="Errors">Collection of error messages.</param>
public record AppendFailure(
    IEnumerable<ReactorConstraintViolation> ConstraintViolations,
    bool HasConcurrencyViolation,
    IEnumerable<string> Errors);
