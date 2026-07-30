// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a single item as it was last observed by a capture, used for diffing against the next observation.
/// </summary>
/// <param name="Key">The value of the capture's key property identifying the item.</param>
/// <param name="Content">The serialized JSON content of the item as it was observed.</param>
public record CaptureObservedItem(string Key, string Content);
