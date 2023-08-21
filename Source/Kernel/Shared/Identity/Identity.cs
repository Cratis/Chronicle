// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Identities;

/// <summary>
/// Represents an identity of something that is responsible for causing a state change.
/// </summary>
/// <param name="Subject">The identifier of the identity, referred to as subject.</param>
/// <param name="Name">Name of the identity.</param>
/// <param name="UserName">Optional username, defaults to empty string.</param>
/// <param name="OnBehalfOf">Optional behalf of <see cref="Identity"/>.</param>
/// <remarks>
/// An identity can be a user, a system, a service or anything else that can be identified.
/// </remarks>
public record Identity(string Subject, string Name, string UserName = "", Identity? OnBehalfOf = default)
{
    /// <summary>
    /// The identity used when not set.
    /// </summary>
    public static readonly Identity NotSet = new("[Not Set]", "[Not Set]");

    /// <summary>
    /// The identity used when the system is the cause.
    /// </summary>
    public static readonly Identity System = new("[System]", "[System]");
}
