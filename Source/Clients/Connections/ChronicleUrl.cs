// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents a URL for connecting to the Chronicle.
/// </summary>
/// <remarks>
/// <![CDATA[
/// chronicle://<host>[:<port>]/?<options>
/// chronicle://username:password@<host>[:<port>]/?<options>
/// chronicle+srv://<host>[:<port>]/?<options>
/// chronicle://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
///
/// // Regex patterns for valid Chronicle URLs:
/// // 1. chronicle://<host>[:<port>]/?<options>
/// const string SingleHostPattern = @"^chronicle:\/\/([a-zA-Z0-9\.-]+)(:\d+)?(\/\?.*)?$";
///
/// // 2. chronicle://username:password@<host>[:<port>]/?<options>
/// const string AuthPattern = @"^chronicle:\/\/([a-zA-Z0-9_\-\.]+):([^@]+)@([a-zA-Z0-9\.-]+)(:\d+)?(\/\?.*)?$";
///
/// // 3. chronicle+srv://<host>[:<port>]/?<options>
/// const string SrvPattern = @"^chronicle\+srv:\/\/([a-zA-Z0-9\.-]+)(:\d+)?(\/\?.*)?$";
///
/// // 4. chronicle://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
/// const string MultiHostPattern = @"^chronicle:\/\/([a-zA-Z0-9\.-]+(:\d+)?)(,([a-zA-Z0-9\.-]+(:\d+)?))*\/(\?.*)?$";
/// ]]>
/// </remarks>
[TypeConverter(typeof(ChronicleUrlConverter))]
public class ChronicleUrl
{
    /// <summary>
    /// The default <see cref="ChronicleUrl"/> pointing to localhost.
    /// </summary>
    public static readonly ChronicleUrl Default = new("chronicle://localhost:35000");

    readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleUrl"/> class.
    /// </summary>
    /// <param name="connectionString">String representation of the connection string.</param>
    public ChronicleUrl(string connectionString)
    {
        _connectionString = connectionString;
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

    /// <inheritdoc/>
    public override string ToString() => _connectionString;
}
