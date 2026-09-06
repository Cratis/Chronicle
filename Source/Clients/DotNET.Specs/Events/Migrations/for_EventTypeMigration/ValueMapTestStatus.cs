// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

/// <summary>
/// The statuses the same external system uses now, on different underlying numbers.
/// </summary>
public enum ValueMapTestStatus
{
    Unspecified = 100,
    Confirmed = 101,
    Withdrawn = 102
}
