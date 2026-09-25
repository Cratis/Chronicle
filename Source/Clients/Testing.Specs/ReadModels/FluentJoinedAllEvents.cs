// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

public record FluentJoinedAllEvents(Guid Id, JoinCustomerId CustomerId, string CustomerName, int EventCount);
