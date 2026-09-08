// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents one rpc method on a service.
/// </summary>
/// <param name="Name">The method name.</param>
/// <param name="InputType">Fully qualified name of the request message.</param>
/// <param name="OutputType">Fully qualified name of the response message.</param>
/// <param name="ClientStreaming">Whether the client streams its requests.</param>
/// <param name="ServerStreaming">Whether the server streams its responses.</param>
public record WireMethod(string Name, string InputType, string OutputType, bool ClientStreaming, bool ServerStreaming)
{
    /// <summary>
    /// Gets the signature as it reads in a proto file, for reporting a change.
    /// </summary>
    public string Signature =>
        $"({(ClientStreaming ? "stream " : string.Empty)}{InputType}) returns ({(ServerStreaming ? "stream " : string.Empty)}{OutputType})";
}
