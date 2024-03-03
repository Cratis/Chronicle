// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Read.Configuration.Microservices;

/// <summary>
/// Represents configuration of a microservice.
/// </summary>
/// <param name="Id">Microservice identifier.</param>
/// <param name="Name">Name of the microservice.</param>
public record Microservice(MicroserviceId Id, string Name);
