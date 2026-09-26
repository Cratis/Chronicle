// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_an_agreement_rpc_fails_temporarily : given.a_decision_reader
{
    Exception _firstError;
    DecisionRead<Model> _secondRead;
    int _listingCalls;

    void Establish() => _readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ =>
        {
            ++_listingCalls;
            if (_listingCalls == 1) throw new InvalidOperationException("Transient RPC failure");
            return new GetDefinitionsResponse
            {
                ReadModels = [new ReadModelDefinition
                {
                    Type = new() { Identifier = typeof(Model).GetReadModelIdentifier() },
                    ObserverIdentifier = "projection", ObserverType = ReadModelObserverType.Projection
                }]
            };
        });

    async Task Because()
    {
        _firstError = await Record.ExceptionAsync(() => _reader.GetDetached<Model>("source"));
        _secondRead = await _reader.GetDetached<Model>("source");
    }

    [Fact] void should_report_the_first_failure() => _firstError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retry_the_agreement() => _listingCalls.ShouldEqual(2);
    [Fact] void should_return_the_subsequent_read() => _secondRead.Exists.ShouldBeTrue();
}
