// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { Guid } from '@cratis/fundamentals';
import { IgnoreRecommendation, PerformRecommendation } from 'Features/Recommendations';
import { a_view_model } from './when_managing_selection/given/a_view_model';

for (const ignore of [false, true]) {
    const Command = ignore ? IgnoreRecommendation : PerformRecommendation;
    const action = ignore ? 'ignoreRecommendations' : 'performRecommendations';
    describe(`when applying a ${action} batch`, () => {
        let context: a_view_model;
        let execute: sinon.SinonStub;
        beforeEach(() => {
            context = new a_view_model();
            context.viewModel.selectedRecommendations = context.recommendations;
            execute = sinon.stub(Command.prototype, 'execute').resolves(CommandResult.empty);
        });
        afterEach(() => sinon.restore());

        it('should apply every captured target with the current event store and namespace', async () => {
            const outcome = await context.viewModel[action]([context.firstRecommendationId, context.secondRecommendationId]);
            outcome.should.deep.equal({ total: 2, failed: 0, failures: [] });
            execute.getCalls().map(call => {
                const command = call.thisValue as PerformRecommendation;
                return [command.eventStore, command.namespace, command.recommendationId];
            }).should.deep.equal([
                ['some-event-store', 'some-namespace', context.firstRecommendationId],
                ['some-event-store', 'some-namespace', context.secondRecommendationId]
            ]);
            context.viewModel.selectedRecommendations.should.have.lengthOf(0);
        });

        it('should do nothing for an empty batch', async () => {
            (await context.viewModel[action]([])).total.should.equal(0);
            execute.should.not.have.been.called;
        });

        it('should continue after a rejected promise and retain only the failed selection', async () => {
            execute.onFirstCall().rejects(new Error('offline'));
            const outcome = await context.viewModel[action]([context.firstRecommendationId, context.secondRecommendationId]);
            outcome.failed.should.equal(1);
            outcome.total.should.equal(2);
            outcome.failures[0].should.contain('offline');
            execute.should.have.been.calledTwice;
            context.viewModel.selectedRecommendations.map(item => item.id).should.deep.equal([context.firstRecommendationId]);
        });

        for (const failure of [CommandResult.failed(['server failure']), CommandResult.validationFailed([]),
            new CommandResult({ ...CommandResult.empty, correlationId: Guid.empty.toString(), isSuccess: false,
                isAuthorized: false, authorizationFailureReason: 'Denied', response: null }, Object, false)]) {
            it('should report an unsuccessful command result and keep the failed target', async () => {
                execute.resolves(failure);
                const outcome = await context.viewModel[action]([context.firstRecommendationId]);
                outcome.failed.should.equal(1);
                outcome.failures.should.have.lengthOf(1);
                context.viewModel.selectedRecommendations.should.have.lengthOf(2);
            });
        }

        it('should preserve newer unrelated selection and snapshot the batch before awaiting', async () => {
            let release!: (result: CommandResult) => void;
            execute.onFirstCall().returns(new Promise<CommandResult>(resolve => { release = resolve; }));
            const ids = [context.firstRecommendationId];
            const pending = context.viewModel[action](ids);
            ids.push(context.secondRecommendationId);
            context.viewModel.selectedRecommendations = [context.recommendations[1]];
            release(CommandResult.empty);
            (await pending).total.should.equal(1);
            execute.should.have.been.calledOnce;
            context.viewModel.selectedRecommendations.map(item => item.id).should.deep.equal([context.secondRecommendationId]);
        });
    });
}
