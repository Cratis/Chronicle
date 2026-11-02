// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>The fluent projection's model.</summary>
/// <typeparam name="TEvent">The event.</typeparam>
/// <param name="Id">The key.</param>
public record ProjectionModel<TEvent>(string Id);
