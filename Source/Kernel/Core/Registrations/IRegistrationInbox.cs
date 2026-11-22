// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Registrations;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Defines the per event store grain that accepts registrations from clients and applies them in the background.
/// </summary>
/// <remarks>
/// A registration is declarative, idempotent and latest-wins, so the kernel can accept it durably and apply it later
/// without losing integrity. Accepting writes one <see cref="RegistrationIntent"/> and answers; a bounded worker
/// applies the intents in the order they were accepted. A client therefore never waits for the work behind its
/// registration, whatever the kernel is busy with. The key of the grain is the event store name.
/// </remarks>
public interface IRegistrationInbox : IGrainWithStringKey
{
    /// <summary>
    /// Starts applying the accepted registrations that have not been applied. Safe to call at any time.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task Ensure();

    /// <summary>
    /// Accepts a registration of projection definitions with a single storage write, and answers without waiting for
    /// it to be applied.
    /// </summary>
    /// <param name="signature">The <see cref="Projections.ProjectionRegistrationSignature"/> of the content, which is its hash.</param>
    /// <param name="definitions">The definitions to register.</param>
    /// <param name="fullSetOwner">The owner whose full set this is, if any.</param>
    /// <param name="registrant">The application registering, if it identified itself.</param>
    /// <param name="version">The registering application's version, if known.</param>
    /// <returns>
    /// The <see cref="RegistrationAcceptance"/>; null when the storage provider cannot keep registration intents, in
    /// which case the caller applies the registration before it answers.
    /// </returns>
    /// <remarks>
    /// A newer registration of a full set supersedes an older one that has not been applied. A registration that
    /// repeats the last one accepted for the same key changes nothing and touches no downstream grain.
    /// </remarks>
    Task<RegistrationAcceptance?> AcceptProjections(string signature, IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version);

    /// <summary>
    /// Gets the state of what a registrant has registered.
    /// </summary>
    /// <param name="kind">The <see cref="RegistrationKind"/> to look at.</param>
    /// <param name="registrant">The registrant, as it identified itself.</param>
    /// <returns>The <see cref="RegistrationStatus"/>.</returns>
    [AlwaysInterleave]
    Task<RegistrationStatus> GetStatus(RegistrationKind kind, string registrant);
}
