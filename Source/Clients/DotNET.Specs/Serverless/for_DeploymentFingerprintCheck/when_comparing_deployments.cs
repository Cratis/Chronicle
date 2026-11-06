// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.Serverless.for_DeploymentFingerprintCheck;

public class when_comparing_deployments
{
    [Theory]
    [InlineData(0UL, "", 0UL, DeploymentFingerprintVerdict.NotRegistered)]
    [InlineData(1UL, "local", 1UL, DeploymentFingerprintVerdict.UpToDate)]
    [InlineData(1UL, "local", 0UL, DeploymentFingerprintVerdict.UpToDate)]
    [InlineData(1UL, "local", 2UL, DeploymentFingerprintVerdict.Stale)]
    [InlineData(2UL, "local", 1UL, DeploymentFingerprintVerdict.Stale)]
    [InlineData(1UL, "different", 1UL, DeploymentFingerprintVerdict.Stale)]
    [InlineData(0UL, "present", 0UL, DeploymentFingerprintVerdict.Stale)]
    [InlineData(1UL, "", 0UL, DeploymentFingerprintVerdict.NotRegistered)]
    [InlineData(1UL, "LOCAL", 1UL, DeploymentFingerprintVerdict.Stale)]
    [InlineData(2UL, "different", 1UL, DeploymentFingerprintVerdict.Stale)]
    public void should_compare_metadata(ulong storedGeneration, string storedFingerprint, ulong localGeneration, object expected) =>
        DeploymentFingerprintCheck.Compare(new EventStoreDeploymentResponse { Generation = storedGeneration, Fingerprint = storedFingerprint }, localGeneration, "local").ShouldEqual((DeploymentFingerprintVerdict)expected);
}
