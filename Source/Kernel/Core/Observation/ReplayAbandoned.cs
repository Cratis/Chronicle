// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a replay was abandoned instead of promoted because a silo could not flush it cleanly.
/// </summary>
/// <remarks>
/// Nothing was promoted: the read model keeps the state it had before the replay started.
/// </remarks>
public class ReplayAbandoned : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayAbandoned"/> class.
    /// </summary>
    public ReplayAbandoned()
        : base("The observer replay was abandoned because a silo could not flush it cleanly")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayAbandoned"/> class.
    /// </summary>
    /// <param name="innerException">The exception that stopped a silo from flushing or from abandoning the replay.</param>
    public ReplayAbandoned(Exception innerException)
        : base("The observer replay was abandoned because a silo could not flush it cleanly", innerException)
    {
    }
}
