// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

public class when_logging_a_classified_partition_with_hashing : given.a_logger_with_identifier_redaction
{
    protected override Telemetry Telemetry => new()
    {
        EventSourceIdsInLogs = IdentifierExposure.Hashed,
        IdentifierHashKey = "chronicle-identifier-hash-key-for-specs-0001"
    };

    void Because() => _logger.QuarantiningFailedPartition("customer-42");

    [Fact] void should_hash_the_partition() => _provider.Messages.Single().ShouldContain("8ce3c6dbf2036756ec5ef13d72ec10c6");
    [Fact] void should_not_expose_the_partition() => _provider.Messages.Single().Contains("customer-42", StringComparison.Ordinal).ShouldBeFalse();
}
