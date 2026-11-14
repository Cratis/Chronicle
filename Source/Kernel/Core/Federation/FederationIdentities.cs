// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Builds and checks the identities of federation peers, <c language="text">spiffe://&lt;trust-domain&gt;/customer/&lt;id&gt;/kernel/&lt;instance&gt;</c>.
/// </summary>
public static partial class FederationIdentities
{
    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}\\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Segment { get; }

    /// <summary>
    /// Check that a value is safe to use as a subject, instance or trust domain: letters, digits, dots, hyphens and underscores, at most 64 characters.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True when the value is a valid segment.</returns>
    public static bool IsValidSegment(string? value) => !string.IsNullOrEmpty(value) && Segment.IsMatch(value) && value is not ("." or "..");

    /// <summary>
    /// Take an identity apart, accepting only exactly <c language="text">spiffe://&lt;trust-domain&gt;/customer/&lt;id&gt;/kernel/&lt;instance&gt;</c>.
    /// </summary>
    /// <param name="identity">The identity URI, as found in a certificate.</param>
    /// <param name="trustDomain">The trust domain, when the identity is well-formed.</param>
    /// <param name="subject">The opaque subject, when the identity is well-formed.</param>
    /// <param name="instance">The kernel instance, when the identity is well-formed.</param>
    /// <returns>True when the identity has exactly the expected form.</returns>
    public static bool TryParse(string? identity, out string trustDomain, out string subject, out string instance)
    {
        trustDomain = subject = instance = string.Empty;
        const string scheme = "spiffe://";
        if (identity?.StartsWith(scheme, StringComparison.Ordinal) != true)
        {
            return false;
        }

        var parts = identity[scheme.Length..].Split('/');
        if (parts.Length != 5 || parts[1] != "customer" || parts[3] != "kernel" || !parts.All(IsValidSegment))
        {
            return false;
        }

        if (!parts[0].Equals(parts[0].ToLowerInvariant(), StringComparison.Ordinal))
        {
            return false;
        }

        trustDomain = parts[0];
        subject = parts[2];
        instance = parts[4];
        return true;
    }

    /// <summary>
    /// Build the identity of a peer.
    /// </summary>
    /// <param name="trustDomain">The trust domain.</param>
    /// <param name="subject">The opaque subject chosen by the operator.</param>
    /// <param name="instance">The kernel instance.</param>
    /// <returns>The identity URI.</returns>
    /// <exception cref="InvalidFederationEnrollment">Thrown when a segment is not valid.</exception>
    public static string Create(string trustDomain, string subject, string instance)
    {
        if (!IsValidSegment(trustDomain) || !trustDomain.Equals(trustDomain.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidFederationEnrollment("the trust domain must be lowercase letters, digits, dots, hyphens and underscores");
        }

        if (!IsValidSegment(subject))
        {
            throw new InvalidFederationEnrollment("the subject must be 1 to 64 letters, digits, dots, hyphens or underscores");
        }

        if (!IsValidSegment(instance))
        {
            throw new InvalidFederationEnrollment("the instance must be 1 to 64 letters, digits, dots, hyphens or underscores");
        }

        return $"spiffe://{trustDomain}/customer/{subject}/kernel/{instance}";
    }
}
