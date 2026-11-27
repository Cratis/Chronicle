// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers;

public partial class ReducerPipeline
{
    async Task EnsureProvenance()
    {
        await ensureProvenance();
        await Sink.EnsureProvenance();
    }
}
