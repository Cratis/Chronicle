// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle;

/// <summary>
/// Resolves the effective server compatibility check policy for the .NET client.
/// </summary>
static class CompatibilityCheckPolicy
{
    /// <summary>
    /// Resolves whether the server compatibility check should be skipped.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/>.</param>
    /// <param name="connectionString"><see cref="ChronicleConnectionString"/>.</param>
    /// <returns><see langword="true"/> when either input asks to skip the check.</returns>
    /// <remarks>
    /// Both inputs default to performing the check, so a client refuses to connect to a server it is not
    /// compatible with unless someone deliberately says otherwise. They therefore combine with OR: skipping
    /// is in effect the moment either input asks for it, so an operator who sets it on either the options or
    /// the connection string is not silently overridden by the other input's untouched default. This is the
    /// mirror of <see cref="TlsCertificateValidationPolicy.ShouldSkip"/>, which defaults to skipping and so
    /// combines with AND instead.
    /// </remarks>
    public static bool ShouldSkip(ChronicleOptions options, ChronicleConnectionString connectionString) =>
        options.SkipCompatibilityCheck || connectionString.SkipCompatibilityCheck;
}
