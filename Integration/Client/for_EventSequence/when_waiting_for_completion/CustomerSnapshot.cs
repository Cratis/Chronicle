// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[FromEvent<CustomerNamed>]
public record CustomerSnapshot(Guid Id, string CustomerName);
