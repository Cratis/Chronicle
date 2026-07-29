// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Defines a manager of identities in the system.
/// </summary>
public interface IIdentityManager
{
    /// <summary>
    /// Renames the name of an identity, identified by its subject.
    /// </summary>
    /// <param name="subject">The subject of the <see cref="Identity"/> to rename.</param>
    /// <param name="name">The new name to give the identity.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The subject is the stable identifier of the identity - the name is the display name and
    /// can change over time, for instance when a person changes their name. Renaming an identity
    /// affects every event and read model that refers to the identity, as the name is resolved
    /// from the identity itself and not stored with what refers to it.
    /// </remarks>
    Task Rename(string subject, string name);
}
