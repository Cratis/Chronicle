// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Models;

public record ModelWithChildren(IEnumerable<ReadModel> Children, EventSequenceNumber __lastHandledEventSequenceNumber);
