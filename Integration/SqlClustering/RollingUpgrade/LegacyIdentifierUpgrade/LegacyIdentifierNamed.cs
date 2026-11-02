// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.LegacyIdentifierUpgrade;

[EventType(EventTypeId)]
public record LegacyIdentifierNamed(string Name)
{
    public const string EventTypeId = "legacy-identifier-named-5512";
}
