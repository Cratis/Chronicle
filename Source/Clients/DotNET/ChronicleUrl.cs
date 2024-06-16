// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents a URL for connecting to the Cratis Kernel.
/// </summary>
/// <remarks>
/// <![CDATA[
/// cratis://<host>[:<port>]/?<options>
/// cratis://username:password@<host>[:<port>]/?<options>
/// cratis+srv://<host>[:<port>]/?<options>
/// cratis://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
/// ]]>
/// </remarks>
public class ChronicleUrl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleUrl"/> class.
    /// </summary>
    /// <param name="connectionString">String representation of the connection string.</param>
    public ChronicleUrl(string connectionString)
    {
        var uri = new Uri(connectionString);
        var port = uri.Port == -1 ? 35000 : uri.Port;
        ServerAddress = new ChronicleServerAddress(uri.Host, port);
    }

    /// <summary>
    /// Gets the <see cref="ChronicleServerAddress"/> for the server.
    /// </summary>
    public ChronicleServerAddress ServerAddress { get; } = new("localhost", 35000);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ChronicleUrl"/>.
    /// </summary>
    /// <param name="connectionString">String connection string to convert from.</param>
    public static implicit operator ChronicleUrl(string connectionString) => new(connectionString);
}
