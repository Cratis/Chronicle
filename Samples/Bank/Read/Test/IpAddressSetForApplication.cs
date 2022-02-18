// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Infrastructure;

namespace Events.Applications
{
    [EventType("d6359a81-b76d-4d3d-ab80-c631acf39a44")]
    public record IpAddressSetForApplication(IpAddress Address);
}
