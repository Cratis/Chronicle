// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Engines.Keys;

/// <summary>
/// Represents the key coming from an event.
/// </summary>
/// <param name="Value">The actual key value.</param>
/// <param name="ArrayIndexers">Any array indexers.</param>
public record Key(object Value, IArrayIndexers ArrayIndexers);
