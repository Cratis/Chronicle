// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Concepts;

namespace Cratis.Chronicle.Integration.Projections.Events;

[EventType]
public record UserAddedToGroup(UserId UserId);
