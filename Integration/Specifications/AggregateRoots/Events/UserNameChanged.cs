// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Specifications.AggregateRoots.Concepts;

namespace Cratis.Chronicle.Integration.Specifications.AggregateRoots.Events;
[EventType]
public record UserNameChanged(UserName NewName);
