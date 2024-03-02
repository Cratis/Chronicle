// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Keys;

/// <summary>
/// Delegate that gets called when building a key for a specific event.
/// </summary>
/// <param name="builder"><see cref="IKeyBuilder{TEvent}"/> that gets passed.</param>
/// <typeparam name="TEvent">Type of event the key builder is for.</typeparam>
public delegate void Key<TEvent>(IKeyBuilder<TEvent> builder);
