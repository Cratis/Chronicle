// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
namespace Cratis.Chronicle.Integration.Specifications.for_Reactors;

[EventType]
public record SomeOtherEvent(int Number);
