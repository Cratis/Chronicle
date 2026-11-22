// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// An event a projection registered while the kernel is busy projects.
/// </summary>
/// <param name="Number">The number the event carries.</param>
[EventType]
public record HeldProjectionEvent(int Number);
