// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels;

[EventType("2d1f20ee-f0fd-5b82-cfa9-cf6eefb23c63")]
public record AnotherEvent(string Value);
