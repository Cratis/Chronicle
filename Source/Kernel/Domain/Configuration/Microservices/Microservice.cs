// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Domain.Configuration.Microservices;

public record Microservice(MicroserviceId Id, string Name);
