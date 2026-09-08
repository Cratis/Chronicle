// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819 // protobuf maps a byte[] property to the proto bytes type; there is no non-array shape for it here

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// The request a client makes on connect to have the server check it against the contracts the server serves.
/// </summary>
/// <remarks>
/// The client sends the descriptor set its contracts package ships rather than one it builds at runtime, because
/// only the .NET and Kotlin generators keep descriptors around at runtime. Shipping it as a build artifact is what
/// lets the check live in one place instead of being reimplemented in every language.
/// </remarks>
[ProtoContract]
public class CompatibilityRequest
{
    /// <summary>
    /// Gets or sets the type of the client, for example <c>.NET</c>, <c>Kotlin</c>, <c>TypeScript</c> or <c>Elixir</c>.
    /// </summary>
    [ProtoMember(1)]
    public string ClientType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the Chronicle client SDK making the connection.
    /// </summary>
    [ProtoMember(2)]
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the contracts the client was built against - the protocol version.
    /// </summary>
    [ProtoMember(3)]
    public string ProtocolVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized <c>FileDescriptorSet</c> describing every contract the client expects.
    /// </summary>
    [ProtoMember(4)]
    public byte[] DescriptorSet { get; set; } = [];
}
