// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Applications;

namespace Events.Applications
{
    [EventType("12e6d722-1e91-4802-86db-00ff3d61d798")]
    public record DeployableCreated(MicroserviceId MicroserviceId, DeployableName Name);
}
