// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a chronicle+srv connection string does not resolve to any DNS SRV records.
/// </summary>
/// <param name="serviceName">The DNS SRV service name that was looked up.</param>
public class NoSrvRecordsFound(string serviceName) : Exception($"No DNS SRV records were found for '{serviceName}'. Verify that the SRV records exist and that the host in the chronicle+srv connection string is correct");
