// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that can provide an updater for updating a constraint index. Typically used by <see cref="IConstraintValidator"/> implementations if they have a constraint index.
/// </summary>
public interface IHaveUpdateConstraintIndex
{
    /// <summary>
    /// Get <see cref="IUpdateConstraintIndex"/> for a <see cref="ConstraintValidationContext"/>.
    /// </summary>
    /// <param name="context"><see cref="ConstraintValidationContext"/> to get for.</param>
    /// <returns>An instance of <see cref="IUpdateConstraintIndex"/> for the context.</returns>
    IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context);
}
