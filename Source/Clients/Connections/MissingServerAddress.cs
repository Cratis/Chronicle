// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a connection string does not contain any server addresses.
/// </summary>
public class MissingServerAddress : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingServerAddress"/> class.
    /// </summary>
    public MissingServerAddress() : base("The connection string does not contain any server addresses")
    {
    }
}
