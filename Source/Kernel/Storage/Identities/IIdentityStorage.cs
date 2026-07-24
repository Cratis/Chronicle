// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Identities;

/// <summary>
/// Defines a system that manages <see cref="Identity">caused by</see> instances.
/// </summary>
public interface IIdentityStorage
{
    /// <summary>
    /// Populate the caused by store.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Populate();

    /// <summary>
    /// Check if a <see cref="Identity"/> exists for a given <see cref="IdentityId"/>.
    /// </summary>
    /// <param name="identityId"><see cref="IdentityId"/> to check.</param>
    /// <returns>True if it does, false if not.</returns>
    Task<bool> HasFor(IdentityId identityId);

    /// <summary>
    /// Get the chain of <see cref="IdentityId"/> for a given <see cref="Identity"/>.
    /// </summary>
    /// <param name="identity"><see cref="Identity"/> to get for.</param>
    /// <returns>Collection representing a chain of <see cref="IdentityId"/>.</returns>
    Task<IImmutableList<IdentityId>> GetFor(Identity identity);

    /// <summary>
    /// Get a <see cref="Identity"/> for a given <see cref="IdentityId"/>.
    /// </summary>
    /// <param name="chain">Collection of <see cref="IdentityId"/> representing the chain to get for.</param>
    /// <returns><see cref="Identity"/> instance.</returns>
    Task<Identity> GetFor(IEnumerable<IdentityId> chain);

    /// <summary>
    /// Get the top level identity for a given <see cref="Identity"/> as <see cref="IdentityId"/> .
    /// </summary>
    /// <param name="identity"><see cref="Identity"/> to get for.</param>
    /// <returns>The <see cref="IdentityId"/> representation for the <see cref="Identity"/>.</returns>
    /// <remarks>
    /// If the <see cref="Identity"/> does not exist, it will be created.
    /// </remarks>
    Task<IdentityId> GetSingleFor(Identity identity);

    /// <summary>
    /// Get a <see cref="Identity"/> for a given <see cref="IdentityId"/>.
    /// </summary>
    /// <param name="identityId"><see cref="IdentityId"/> to get for.</param>
    /// <returns><see cref="Identity"/> instance.</returns>
    Task<Identity> GetSingleFor(IdentityId identityId);

    /// <summary>
    /// Rename the display name of the <see cref="Identity"/> identified by the given subject.
    /// </summary>
    /// <param name="subject">The subject that uniquely identifies the <see cref="Identity"/> to rename.</param>
    /// <param name="name">The new name to set.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only the display name is changed — the subject, username and on-behalf-of chain are preserved.
    /// Chronicle keys PII encryption on the compliance subject, never on the display name, so renaming
    /// an identity does not affect any encryption-key lookup.
    /// </remarks>
    Task Rename(string subject, string name);

    /// <summary>
    /// Get all identities.
    /// </summary>
    /// <returns>Collection of <see cref="Identity"/>.</returns>
    Task<IEnumerable<Identity>> GetAll();

    /// <summary>
    /// Observe all identities.
    /// </summary>
    /// <returns>An observable of collection of <see cref="Identity"/>.</returns>
    ISubject<IEnumerable<Identity>> ObserveAll();
}
