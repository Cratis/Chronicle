// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_delivery_timeout_is_not_positive : Specification
{
    IReadOnlyList<string> _zero;
    IReadOnlyList<string> _negative;

    void Because()
    {
        _zero = Validate(TimeSpan.Zero);
        _negative = Validate(TimeSpan.FromSeconds(-1));
    }

    [Fact] void should_fail_once_for_zero() => _zero.Count.ShouldEqual(1);
    [Fact] void should_name_the_timeout_for_zero() => _zero[0].Contains("Cratis:Chronicle:Serverless:Delivery:Timeout").ShouldBeTrue();
    [Fact] void should_fail_once_for_negative() => _negative.Count.ShouldEqual(1);
    [Fact] void should_name_the_timeout_for_negative() => _negative[0].Contains("Cratis:Chronicle:Serverless:Delivery:Timeout").ShouldBeTrue();

    static IReadOnlyList<string> Validate(TimeSpan timeout) =>
        ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { DeploymentGeneration = 1, Delivery = { Timeout = timeout } } }, "Production");
}
