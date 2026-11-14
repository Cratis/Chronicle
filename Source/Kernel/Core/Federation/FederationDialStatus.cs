// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The dialer-side state of the session to the authority: the outbound connection this kernel keeps open after it was enrolled.
/// </summary>
/// <param name="Connected">Whether the session is open and the last operation on it succeeded.</param>
/// <param name="LastExchange">When a pull was last answered, an inbound batch was last delivered or a renewal last succeeded, or null when none has.</param>
/// <param name="LastError">The last failure, scrubbed and never containing a secret, or null when none has happened or a later exchange succeeded.</param>
public record FederationDialStatus(bool Connected, DateTimeOffset? LastExchange, string? LastError)
{
    /// <summary>
    /// Gets the status of a kernel that has not exchanged anything with an authority.
    /// </summary>
    public static FederationDialStatus None { get; } = new(false, null, null);
}
