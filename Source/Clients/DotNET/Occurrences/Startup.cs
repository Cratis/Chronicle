// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Represents the durable start of one logical activation, not the start of every instance.
/// </summary>
[Trigger("startup")]
public record Startup;
