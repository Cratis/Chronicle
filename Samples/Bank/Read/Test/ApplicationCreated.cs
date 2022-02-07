// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Applications;
using Concepts.Azure;
using Concepts.Organizations;
using ApplicationId = Concepts.Applications.ApplicationId;

namespace Events.Applications
{
    [EventType("689de713-b3b8-4f09-9943-ebf959667852")]
    public record ApplicationCreated(ApplicationName Name, AzureSubscriptionId AzureSubscriptionId, CloudLocationKey CloudLocation);
}
