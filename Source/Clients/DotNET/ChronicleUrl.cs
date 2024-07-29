// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents a URL for connecting to the Cratis Kernel.
/// </summary>
/// <remarks>
/// <![CDATA[
/// chronicle://<host>[:<port>]/?<options>
/// chronicle://username:password@<host>[:<port>]/?<options>
/// chronicle+srv://<host>[:<port>]/?<options>
/// chronicle://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
/// ]]>
/// </remarks>
public class ChronicleUrl
{
    /// <summary>
    /// The default <see cref="ChronicleUrl"/> pointing to localhost.
    /// </summary>
    public static readonly ChronicleUrl Default = new("chronicle://localhost:35000");

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
