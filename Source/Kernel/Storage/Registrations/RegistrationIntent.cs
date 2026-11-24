// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Registrations;

/// <summary>
/// Represents the durable record of a registration the kernel has accepted: one per event store, kind and key.
/// </summary>
/// <param name="Kind">The <see cref="RegistrationKind"/> of the artifacts.</param>
/// <param name="Key">The key the intent is coalesced under: the registrant for a full set, the registrant and the content hash otherwise.</param>
/// <param name="Registrant">The application that registered, if it identified itself.</param>
/// <param name="Revision">The revision the intent was accepted as. Revisions increase within an event store.</param>
/// <param name="Hash">The hash of the content of the registration.</param>
/// <param name="State">The <see cref="RegistrationIntentState"/> of the intent.</param>
/// <param name="Payload">The serialized registration, until it has been applied.</param>
/// <param name="FullSetOwner">The owner whose full set the registration is, if any.</param>
/// <param name="Version">The version of the registering application, if known.</param>
/// <param name="Attempts">How many times applying the registration has been attempted.</param>
/// <param name="Failure">Why the registration could not be applied, when it could not.</param>
/// <param name="UpdatedAt">When the intent last changed.</param>
public record RegistrationIntent(
    RegistrationKind Kind,
    string Key,
    string Registrant,
    long Revision,
    string Hash,
    RegistrationIntentState State,
    ReadOnlyMemory<byte> Payload,
    int? FullSetOwner,
    string? Version,
    int Attempts,
    string? Failure,
    DateTimeOffset UpdatedAt);
