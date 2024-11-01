// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;

public record UserInternalState(StateProperty<UserName> Name, StateProperty<bool> Deleted);
