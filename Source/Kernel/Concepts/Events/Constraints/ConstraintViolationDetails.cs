// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents details related to a constraint violation.
/// </summary>
public class ConstraintViolationDetails : Dictionary<string, string>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConstraintViolationDetails"/>.
    /// </summary>
    public ConstraintViolationDetails()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConstraintViolationDetails"/>.
    /// </summary>
    /// <param name="details">Details to initialize with.</param>
    public ConstraintViolationDetails(IDictionary<string, string> details) : base(details)
    {
    }
}
