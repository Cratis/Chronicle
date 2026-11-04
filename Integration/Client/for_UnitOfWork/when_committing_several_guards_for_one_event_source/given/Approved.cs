// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source.given;

[EventType("several-guards-approved")]
public record Approved;
