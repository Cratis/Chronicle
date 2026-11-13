// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { given } from 'given';
import strings from 'Strings';
import { a_view_model } from '../given/a_view_model';

const unauthorized = new CommandResult({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString(), isSuccess: false, isAuthorized: false, response: undefined }, Object, false);
const unsuccessful = new CommandResult({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString(), isSuccess: false, response: undefined }, Object, false);

for (const [name, result, expected] of [
    ['authorization is denied', unauthorized, 'not authorized'],
    ['validation fails', CommandResult.validationFailed([new ValidationResult(ValidationResultSeverity.Error, 'No applicable constraints', [], {})]), 'No applicable constraints'],
    ['the server reports an exception', CommandResult.failed(['Server unavailable']), 'Server unavailable'],
    ['the request is unsuccessful', unsuccessful, 'could not be started'],
    ['the response has no job id', CommandResult.empty, 'could not be started']
] as const) {
    describe(`when reindexing and ${name}`, given(a_view_model, context => {
        beforeEach(async () => {
            await context.request();
            context.execute.resolves(result);
            await context.request();
        });

        it('should report the failure', () => context.viewModel.feedback.should.contain(expected));
        it('should not retain a stale job link', () => (context.viewModel.jobId === undefined).should.be.true);
        it('should release the pending state', () => context.viewModel.isReindexing.should.be.false);
    }));
}

describe('when reindexing and the request throws', given(a_view_model, context => {
    beforeEach(async () => {
        context.execute.rejects(new Error('Network unavailable'));
        await context.request();
    });

    it('should report the localized failure', () => context.viewModel.feedback.should.equal(strings.eventStore.namespaces.constraints.failed));
    it('should not expose exception details', () => context.viewModel.feedback.should.not.contain('Network unavailable'));
    it('should not offer a job link', () => (context.viewModel.jobId === undefined).should.be.true);
    it('should release the pending state', () => context.viewModel.isReindexing.should.be.false);
}));
