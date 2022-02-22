// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Exception that gets thrown when the <see cref="ConceptSerializer{T}"/> failed serializing a concept.
/// </summary>
public class FailedConceptSerialization : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedConceptSerialization"/> class.
    /// </summary>
    /// <param name="msg">Message to display.</param>
    public FailedConceptSerialization(string msg)
        : base(msg)
    {
    }
}
