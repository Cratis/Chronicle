// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace TestApp;

/// <summary>
/// A read model with a property mapped to an event property.
/// </summary>
/// <param name="Count">The count mapped to the event property.</param>
public record SomeReadModel(
    [Count<MyEvent>]
    int Count);

