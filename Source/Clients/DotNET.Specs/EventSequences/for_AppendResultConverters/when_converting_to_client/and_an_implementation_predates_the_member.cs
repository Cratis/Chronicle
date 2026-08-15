// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters.when_converting_to_client;

/// <summary>
/// <see cref="IAppendResult.ConcurrencyCheckPerformed"/> has a default implementation, so adding it did not break
/// anyone who had already implemented the interface. An implementation that predates it reports the check as not
/// performed, which under-reports rather than promising a guarantee it cannot speak for - the same direction the
/// wire takes when a kernel is too old to send the field.
/// </summary>
public class and_an_implementation_predates_the_member : Specification
{
    IAppendResult _result;

    void Because() => _result = new an_append_result_written_before_the_member_existed();

    [Fact] void should_report_the_check_as_not_performed() => _result.ConcurrencyCheckPerformed.ShouldBeFalse();

    class an_append_result_written_before_the_member_existed : IAppendResult
    {
        public CorrelationId CorrelationId => CorrelationId.NotSet;
        public bool IsSuccess => true;
        public bool HasConstraintViolations => false;
        public bool HasConcurrencyViolations => false;
        public bool HasErrors => false;
        public IEnumerable<ConstraintViolation> ConstraintViolations => [];
        public IEnumerable<AppendError> Errors => [];
    }
}
