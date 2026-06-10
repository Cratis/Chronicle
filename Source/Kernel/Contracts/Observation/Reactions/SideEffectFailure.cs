// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Reactors;

/// <summary>
/// Represents a failure from an individual append operation in a reactor side-effect.
/// </summary>
[ProtoContract]
public class SideEffectFailure
{
    /// <summary>
    /// Gets or sets the constraint violations.
    /// </summary>
    [ProtoMember(1)]
    public IList<ConstraintViolation> ConstraintViolations { get; set; } = [];

    /// <summary>
    /// Gets or sets whether a concurrency violation occurred.
    /// </summary>
    [ProtoMember(2)]
    public bool HasConcurrencyViolation { get; set; }

    /// <summary>
    /// Gets or sets the error messages.
    /// </summary>
    [ProtoMember(3)]
    public IList<string> Errors { get; set; } = [];
}
