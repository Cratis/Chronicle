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
public class CratisUrl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CratisUrl"/> class.
    /// </summary>
    /// <param name="connectionString">String representation of the connection string.</param>
    public CratisUrl(string connectionString)
    {
        var uri = new Uri(connectionString);
        var port = uri.Port == -1 ? 35000 : uri.Port;
        ServerAddress = new CratisServerAddress(uri.Host, port);
    }

    /// <summary>
    /// Gets the <see cref="CratisServerAddress"/> for the server.
    /// </summary>
    public CratisServerAddress ServerAddress { get; } = new("localhost", 35000);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CratisUrl"/>.
    /// </summary>
    /// <param name="connectionString">String connection string to convert from.</param>
    public static implicit operator CratisUrl(string connectionString) => new(connectionString);
}
