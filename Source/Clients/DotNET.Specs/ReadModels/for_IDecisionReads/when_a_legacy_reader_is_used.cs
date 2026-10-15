// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_IDecisionReads;

public class when_a_legacy_reader_is_used : Specification
{
    IDecisionReads _reader;

    void Establish() => _reader = new LegacyReader();

    [Fact] void should_refuse_stream_admission() => ((DecisionReadRefused)Catch.Exception(() => _reader.AdmitForStream<Model>())).Reason.ShouldEqual(DecisionReadRefusalReason.StreamScopeNotSupported);
    [Fact] async Task should_refuse_a_detached_stream_read() => ((DecisionReadRefused)await Catch.Exception(() => _reader.GetDetached<Model>("source", "type", "stream"))).Reason.ShouldEqual(DecisionReadRefusalReason.StreamScopeNotSupported);
    [Fact] async Task should_refuse_an_enrolled_stream_read() => ((DecisionReadRefused)await Catch.Exception(() => _reader.Get<Model>("source", "type", "stream"))).Reason.ShouldEqual(DecisionReadRefusalReason.StreamScopeNotSupported);

    class Model;

    class LegacyReader : IDecisionReads
    {
        public Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
            where T : class => GetDetached<T>(key, cancellationToken);

        public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
            where T : class => Task.FromResult(DecisionRead<T>.Unprotected(key, null));

        public DecisionReadAdmission Admit<T>()
            where T : class => new(false, DecisionReadRefusalReason.StreamScopeNotSupported);
    }
}
