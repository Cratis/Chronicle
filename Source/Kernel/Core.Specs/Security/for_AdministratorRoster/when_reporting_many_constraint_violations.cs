// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_reporting_many_constraint_violations : Specification
{
    string _description;

    void Because()
    {
        var violation = new ConstraintViolation(
            "SyntheticEvent",
            EventSequenceNumber.BeforeFirst,
            ConstraintType.Schema,
            "SchemaValidation",
            new string('x', 10000),
            new() { ["path"] = "#/userId" });
        _description = AdministratorRosterLogging.DescribeConstraintViolations(Enumerable.Repeat(violation, 100), ["SyntheticEvent"]);
    }

    [Fact] void should_bound_the_diagnostic() => (_description.Length < 2500).ShouldBeTrue();
    [Fact] void should_identify_the_constraint() => _description.Contains("Schema/SchemaValidation", StringComparison.Ordinal).ShouldBeTrue();
}
