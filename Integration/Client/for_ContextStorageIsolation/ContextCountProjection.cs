// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContextStorageIsolation;

/// <summary>
/// Counts recorded events per event source.
/// </summary>
public class ContextCountProjection : IProjectionFor<ContextCount>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ContextCount> builder) => builder
        .From<ContextEventRecorded>(_ => _.Count(model => model.Count));
}
