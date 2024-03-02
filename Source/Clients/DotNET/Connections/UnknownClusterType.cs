// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Connections;

/// <summary>
/// Exception that gets thrown when client has been configured with an unknown cluster type.
/// </summary>
public class UnknownClusterType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownClusterType"/> class.
    /// </summary>
    public UnknownClusterType() : base("Unknown cluster type in configuration")
    {
    }
}
