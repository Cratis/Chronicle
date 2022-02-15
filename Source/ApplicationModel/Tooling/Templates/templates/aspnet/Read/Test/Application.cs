// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Applications;
using Concepts.Azure;

namespace Read.Applications
{
    public record Application(
        Concepts.Applications.ApplicationId Id,
        ApplicationName Name,
        AzureSubscriptionId AzureSubscriptionId,
        CloudLocationKey CloudLocation,
        IEnumerable<MicroserviceOnApplication> Microservices);
}
