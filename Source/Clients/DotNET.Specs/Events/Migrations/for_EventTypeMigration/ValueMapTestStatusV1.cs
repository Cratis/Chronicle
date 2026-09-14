// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

/// <summary>
/// The statuses an external system used before it renumbered them.
/// </summary>
public enum ValueMapTestStatusV1
{
    Unknown = 0,
    Verified = 1,
    Revoked = 2
}
