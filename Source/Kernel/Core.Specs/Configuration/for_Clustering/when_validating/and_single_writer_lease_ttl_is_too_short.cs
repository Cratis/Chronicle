// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_validating;

public class and_single_writer_lease_ttl_is_too_short : Specification
{
    ChronicleOptions _options;
    IEnumerable<ValidationResult> _errors;

    void Establish()
    {
        _options = new();
        _options.Clustering.SingleWriterLease.Ttl = TimeSpan.FromSeconds(4);
    }

    void Because() => _errors = _options.Validate(new ValidationContext(_options)).ToArray();

    [Fact] void should_reject_the_configuration() => _errors.ShouldContain(error => error.MemberNames.Contains("Clustering.SingleWriterLease.Ttl"));
}
