// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_the_resolver_is_not_known_to_be_io_free : given.a_page_pipeline
{
    KeyResolver _resolver;
    int _calls;
    int _callsAfterPrepare;

    void Establish()
    {
        _resolver = (_, _, _) =>
        {
            _calls++;
            return Task.FromResult<KeyResolverResult>(new ResolvedKey(new Key("root-0", ArrayIndexers.NoIndexers)));
        };
        _projection.GetKeyResolverFor(_childType).Returns(_resolver);
    }

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _callsAfterPrepare = _calls;
        await HandlePage(_pipeline);
    }

    [Fact] void should_not_run_unknown_resolvers_during_preparation() => _callsAfterPrepare.ShouldEqual(0);
    [Fact] void should_leave_them_on_the_normal_path() => _calls.ShouldEqual(4);
}
