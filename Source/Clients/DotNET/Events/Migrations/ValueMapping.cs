// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents a single value in a migration's value map and what that value becomes on the other side.
/// </summary>
/// <param name="From">The value as it appears in the source generation.</param>
/// <param name="To">The value it becomes in the target generation.</param>
public record ValueMapping(object From, object To);
