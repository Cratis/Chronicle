// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Events;

[EventType]
public record UserRemovedFromGroup(UserId UserId);
