// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Versioning;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents the information sent to the Kernel when connecting.
/// </summary>
/// <param name="MicroserviceId"><see cref="MicroserviceId"/> for the current microservice the client is for.</param>
/// <param name="ConnectionId">The unique identifier of the connection.</param>
/// <param name="ClientVersion">The version of the client.</param>
public record ClientInformation(MicroserviceId MicroserviceId, ConnectionId ConnectionId, SemVer ClientVersion);
