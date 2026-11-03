// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_restarting;

public class and_an_existing_model_changes : given.watched_reactors
{
    async Task Because()
    {
        await Send(ReadModelChangeType.Added);
        _reactors.Dispose();
        _reactors.Start();
        await Send(ReadModelChangeType.Modified);
    }

    [Fact] void should_not_infer_an_addition_after_restarting() => _recorded.Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified]);
}
