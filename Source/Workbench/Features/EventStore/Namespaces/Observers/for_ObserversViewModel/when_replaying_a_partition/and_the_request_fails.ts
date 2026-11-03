// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

const unauthorized = new CommandResult({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString(), isSuccess: false, isAuthorized: false, response: undefined }, Object, false);
const unsuccessful = new CommandResult({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString(), isSuccess: false, response: undefined }, Object, false);

for (const [name, result, expected] of [
    ['authorization is denied', unauthorized, 'not authorized'],
    ['validation fails', CommandResult.validationFailed([new ValidationResult(ValidationResultSeverity.Error, 'Invalid partition', [], {})]), 'Invalid partition'],
    ['the server reports an exception', CommandResult.failed(['Server unavailable']), 'Server unavailable'],
    ['the request is unsuccessful', unsuccessful, 'not accepted']
] as const) {
    describe(`when replaying a partition and ${name}`, given(a_view_model, context => {
        beforeEach(async () => {
            context.execute.resolves(result);
            await context.request();
        });

        it('should show useful failure feedback', () => context.viewModel.partitionReplayFeedback.should.contain(expected));
        it('should not claim acceptance', () => context.viewModel.partitionReplayAccepted.should.be.false);
        it('should preserve the input', () => context.viewModel.partitionEventSourceId.should.equal(' Customer-A/1,Two '));
        it('should release the pending state', () => context.viewModel.isReplayingPartition.should.be.false);
        it('should permit retry', () => context.viewModel.canReplayPartition.should.be.true);
    }));
}
