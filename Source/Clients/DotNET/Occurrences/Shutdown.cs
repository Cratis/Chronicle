// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Represents an explicitly ended activation, delivered at least once only after it was recorded.
/// </summary>
[Trigger("shutdown")]
public record Shutdown;
