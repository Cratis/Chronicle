// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that gets thrown when a call needs the Chronicle connection while a recent attempt to establish it failed.
/// </summary>
public class ConnectionUnavailable : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionUnavailable"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string text the client is configured with. It is redacted before it is used in the message.</param>
    public ConnectionUnavailable(string connectionString)
        : base(CreateMessage(ChronicleConnectionString.Redact(connectionString)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionUnavailable"/> class.
    /// </summary>
    /// <param name="connectionString">The <see cref="ChronicleConnectionString"/> the client is configured with.</param>
    public ConnectionUnavailable(ChronicleConnectionString connectionString)
        : base(CreateMessage(connectionString.Redacted))
    {
    }

    static string CreateMessage(string redacted) =>
        $"The Chronicle connection to '{redacted}' is unavailable - the last attempt to connect failed and the client is retrying in the background. Retry the call, or report the client as unhealthy.";
}
