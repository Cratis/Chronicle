// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations;

/// <summary>
/// Represents the row of a registration intent: one per kind and key.
/// </summary>
public class RegistrationIntentEntry
{
    /// <summary>
    /// Gets or sets the identifier of the row: the hash of the kind and the key, so that a registrant of any length can be kept.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of the registration.
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Gets or sets the key the intent is coalesced under.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the registrant.
    /// </summary>
    public string Registrant { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revision the intent was accepted as.
    /// </summary>
    public long Revision { get; set; }

    /// <summary>
    /// Gets or sets the hash of the content of the registration.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state of the intent.
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// Gets or sets the serialized registration, until it has been applied.
    /// </summary>
#pragma warning disable CA1819 // The column holds the bytes of the serialized registration.
    public byte[] Payload { get; set; } = [];
#pragma warning restore CA1819

    /// <summary>
    /// Gets or sets the owner whose full set the registration is, if any.
    /// </summary>
    public int? FullSetOwner { get; set; }

    /// <summary>
    /// Gets or sets the version of the registering application, if known.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets how many times applying the registration has been attempted.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets why the registration could not be applied, when it could not.
    /// </summary>
    public string? Failure { get; set; }

    /// <summary>
    /// Gets or sets when the intent last changed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
