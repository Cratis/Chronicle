// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities;

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
    public static readonly Identity NotSet = new("1efc9b81-0612-4466-962c-86acc4e9a028", "[Not Set]", "[Not Set]");

    /// <summary>
    /// The identity used when the identity is not known.
    /// </summary>
    public static readonly Identity Unknown = new("3321cf62-db16-425e-8173-99fcfefe11dd", "[Unknown]", "[Unknown]");

    /// <summary>
    /// The identity used when the system is the cause.
    /// </summary>
    public static readonly Identity System = new("5d032c92-9d5e-41eb-947a-ee5314ed0032", "[System]", "[System]");
}
