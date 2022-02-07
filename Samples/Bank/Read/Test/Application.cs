// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Applications;
using Concepts.Azure;
using Concepts.Organizations;
using Events.Applications;
using ApplicationId = Concepts.Applications.ApplicationId;

namespace Read.Applications
{
    public record Application(
        ApplicationId Id,
        ApplicationName Name,
        AzureSubscriptionId AzureSubscriptionId,
        CloudLocationKey CloudLocation,
        IEnumerable<MicroserviceOnApplication> Microservices);
}
