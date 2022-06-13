// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Integration;

[EventType("c6ce97b7-4f81-44b2-9800-8396fe319b1e", isPublic: true)]
public record SomePublicEvent(int SomeInteger, string SomeString);
