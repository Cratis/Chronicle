// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that gets thrown when a call needs the Chronicle connection while a recent attempt to establish it failed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectionUnavailable"/> class.
/// </remarks>
/// <param name="connectionString">The redacted connection string the client is configured with.</param>
public class ConnectionUnavailable(string connectionString)
    : Exception($"The Chronicle connection to '{connectionString}' is unavailable - the last attempt to connect failed and the client is retrying in the background. Retry the call, or report the client as unhealthy.");
