// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Projections;

/// <summary>
/// Represents a combination of <see cref="EventType"/> and <see cref="KeyResolver"/>.
/// </summary>
/// <param name="EventType">The <see cref="EventType"/>.</param>
/// <param name="KeyResolver">The <see cref="KeyResolver"/> for resolving the key.</param>
public record EventTypeWithKeyResolver(EventType EventType, KeyResolver KeyResolver);
