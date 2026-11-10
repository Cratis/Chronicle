// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal enum FaultKind
{
    KillKernel,
    KillMongo,
    StopKernel,
    StopMongo,
    KillProducer,
    StopProducer,
    KillConsumer,
    StopConsumer
}
