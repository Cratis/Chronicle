// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Execution
{
    /// <summary>
    /// Represents an identifier of an identity that was the root of a cause.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record CausedBy(Guid Value) : ConceptAs<Guid>(Value);

}
