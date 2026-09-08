// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Converters for converting to/from contracts for identities.
/// </summary>
/// <remarks>
/// These live beside the identity artifacts rather than inside a service, because the conversion is shared: an
/// identity travels inside an event context, an appended event and an event revision, none of which belong to
/// the identity area.
/// </remarks>
internal static class IdentityConverters
{
    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="identities">Identities to convert.</param>
    /// <returns>Converted Identities.</returns>
    public static IEnumerable<Contracts.Identities.Identity> ToContract(this IEnumerable<Concepts.Identities.Identity> identities) =>
        identities.Select(_ => _.ToContract());

    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="identity">Identity to convert.</param>
    /// <returns>Converted Identity.</returns>
    public static Contracts.Identities.Identity ToContract(this Concepts.Identities.Identity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };

    /// <summary>
    /// Convert to the read model the identity queries answer with.
    /// </summary>
    /// <param name="identities">Identities to convert.</param>
    /// <returns>The identities as read models.</returns>
    /// <remarks>
    /// Materialized, because what leaves here is serialized from its runtime type and a lazily projected sequence
    /// keeps the source type as its first generic argument. See EventStoreNames for what that cost the last time.
    /// </remarks>
    public static IEnumerable<IdentityDetails> ToDetails(this IEnumerable<Concepts.Identities.Identity> identities) =>
        [.. identities.Select(_ => new IdentityDetails(_.Subject, _.Subject, _.Name, _.UserName, _.OnBehalfOf?.ToContract()))];

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="identity"><see cref="Contracts.Identities.Identity"/> to convert from.</param>
    /// <returns>Converted <see cref="Concepts.Identities.Identity"/>.</returns>
    public static Concepts.Identities.Identity ToChronicle(this Contracts.Identities.Identity identity) =>
        new(identity.Subject, identity.Name, identity.UserName, identity.OnBehalfOf?.ToChronicle());
}
