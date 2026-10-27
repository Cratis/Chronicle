// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

/// <summary>
/// An externally extensible record exercises the public inheritance surface.
/// </summary>
public record SampleExtensibleContract
{
    /// <summary>
    /// A constant available to derived types.
    /// </summary>
    protected const int Limit = 7;

    /// <summary>
    /// Gets or sets a value that derived types can change.
    /// </summary>
    public string Value { get; protected set; } = string.Empty;

    /// <summary>
    /// Describes the record for derived types and this assembly.
    /// </summary>
    /// <returns>The current value.</returns>
    protected internal virtual string Describe() => Hidden();

    internal string InternalMember() => Value;

    private protected string PrivateProtectedMember() => Value;

    string Hidden() => Value;
}
