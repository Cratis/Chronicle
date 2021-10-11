// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Extensions.Dolittle.EventStore
{
    public record Version(uint Major, uint Minor, uint Patch, uint Build, string PreRelease);
}
