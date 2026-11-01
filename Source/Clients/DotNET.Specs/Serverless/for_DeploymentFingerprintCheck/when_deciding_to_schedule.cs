// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_DeploymentFingerprintCheck;

public class when_deciding_to_schedule
{
    [Theory]
    [InlineData(false, false, true, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    public void should_only_enable_for_deploy(bool serverless, bool coldStart, bool enabled, bool expected)
    {
        var options = new ChronicleOptions();
        options.Serverless.Mode = serverless ? ServerlessMode.Serverless : ServerlessMode.Off;
        options.Serverless.Registration = coldStart ? ServerlessRegistration.OnColdStart : ServerlessRegistration.Deploy;
        options.Serverless.CheckArtifactFingerprint = enabled;
        DeploymentFingerprintCheck.IsEnabled(options).ShouldEqual(expected);
    }

    [Fact] void should_default_to_enabled() => new ChronicleServerlessOptions().CheckArtifactFingerprint.ShouldBeTrue();
}
