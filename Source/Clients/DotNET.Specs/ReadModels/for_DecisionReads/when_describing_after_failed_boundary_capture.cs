// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using NSubstitute.ExceptionExtensions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_describing_after_failed_boundary_capture : given.a_decision_reader
{
    Exception _error;
    DecisionReadDescription _description;
    int _callsBeforeDescription;
    int _callsAfterDescription;

    void Establish() => _sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>(), Arg.Any<CallContext>()).ThrowsAsync(new IOException("boundary unavailable"));

    async Task Because()
    {
        _error = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
        _callsBeforeDescription = _sequences.ReceivedCalls().Count();
        _description = _reader.Describe<Model>();
        _callsAfterDescription = _sequences.ReceivedCalls().Count();
    }

    [Fact] void should_report_capture_failure_instead_of_fabricating_an_empty_boundary() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_fold_after_a_failed_capture() => _folds.ShouldEqual(0);
    [Fact] void should_still_describe_the_local_shape() => _description.Admission.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_retry_the_boundary_for_description() => _callsAfterDescription.ShouldEqual(_callsBeforeDescription);
}
