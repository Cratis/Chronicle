// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Extensions.Dolittle.EventStore
{
    public record ExecutionContext(Guid Correlation, Guid Microservice, Guid Tenant, Version Version, string Environment, Claim[] Claims);
}
