// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that gets thrown when the client URI is not possible to resolve from ASP.NET Core.
/// </summary>
public class UnableToResolveClientUri : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnableToResolveClientUri"/> class.
    /// </summary>
    public UnableToResolveClientUri() : base("Unable to resolve client URI. This could be because the Cratis Client is being instantiated before ASP.NET has finished setting up its pipeline, for instance in a HostedService.")
    {
    }
}
