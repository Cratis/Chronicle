// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { UnignoreRecommendation } from 'Features/Recommendations';
import { RecommendationsViewModel } from '../../../RecommendationViewModel';

class TestableRecommendationsViewModel extends RecommendationsViewModel {
    commands: UnignoreRecommendation[] = [];

    protected createUnignoreRecommendation(): UnignoreRecommendation {
        const command = new UnignoreRecommendation();
        this.commands.push(command);
        return command;
    }
}

export class a_view_model_with_ignored_recommendations {
    firstRecommendationId = Guid.parse('11111111-1111-1111-1111-111111111111');
    secondRecommendationId = Guid.parse('22222222-2222-2222-2222-222222222222');
    viewModel = new TestableRecommendationsViewModel({ eventStore: 'some-event-store', namespace: 'some-namespace' });
    error: Error | undefined;

    constructor() {
        this.viewModel.showIgnored = true;
        this.viewModel.selectAllRecommendations([this.firstRecommendationId, this.secondRecommendationId]);
    }

    // The command at the given position in the call order fails with the message when failing is set.
    answerWith(failure: { index: number; message: string } | undefined) {
        let call = 0;
        sinon.stub(UnignoreRecommendation.prototype, 'execute').callsFake(() => {
            const index = call++;
            const failed = failure?.index === index;
            return Promise.resolve({
                validationResults: [],
                exceptionMessages: failed ? [failure.message] : [],
                onFailed: (callback: () => void) => {
                    if (failed) callback();
                }
            } as never);
        });
    }

    async unignore() {
        try {
            await this.viewModel.unignoreRecommendations([this.firstRecommendationId, this.secondRecommendationId]);
        } catch (error) {
            this.error = error as Error;
        }
    }
}
