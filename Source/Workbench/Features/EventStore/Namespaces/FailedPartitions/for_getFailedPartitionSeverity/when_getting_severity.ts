// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartitionSeverity } from '../FailedPartitionSeverity';
import { FailedPartitionStatus } from '../FailedPartitionStatus';
import { getFailedPartitionSeverity } from '../getFailedPartitionSeverity';

describe('when getting severity', () => {
    it('should make quarantined failures critical', () =>
        getFailedPartitionSeverity(FailedPartitionStatus.Quarantined).should.equal(
            FailedPartitionSeverity.Danger,
        ));
    it('should make automatically retryable failures warnings', () =>
        getFailedPartitionSeverity(FailedPartitionStatus.Failed).should.equal(
            FailedPartitionSeverity.Warning,
        ));
    it('should make unknown state secondary', () =>
        getFailedPartitionSeverity(FailedPartitionStatus.Unknown).should.equal(
            FailedPartitionSeverity.Secondary,
        ));
    it('should make resolved state successful', () =>
        getFailedPartitionSeverity(FailedPartitionStatus.Resolved).should.equal(
            FailedPartitionSeverity.Success,
        ));
});
