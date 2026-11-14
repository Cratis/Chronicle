// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

public class when_logging_a_classified_partition : given.a_logger_with_identifier_redaction
{
    void Because() => _logger.QuarantiningFailedPartition("customer-42");

    [Fact] void should_redact_the_partition() => _provider.Messages.Single().ShouldContain("[redacted]");
    [Fact] void should_not_expose_the_partition() => _provider.Messages.Single().Contains("customer-42", StringComparison.Ordinal).ShouldBeFalse();
}
