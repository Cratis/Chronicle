// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
public class InactiveRecordingReactor<T>(InactiveReducerFrames frames) : IReadModelReactor
{
    public void Added(ReducedModel model, EventContext context) => frames.Record(ReadModelChangeType.Added, context);
    public void Modified(ReducedModel model, EventContext context) => frames.Record(ReadModelChangeType.Modified, context);
    public void Removed(IEnumerable<ReducedModel> models, EventContext context) => frames.Record(ReadModelChangeType.Removed, context);
}
