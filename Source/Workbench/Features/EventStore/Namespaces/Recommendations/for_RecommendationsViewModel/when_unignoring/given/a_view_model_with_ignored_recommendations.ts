// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { UnignoreRecommendation } from 'Features/Recommendations';
import { a_view_model } from '../../when_managing_selection/given/a_view_model';

export class a_view_model_with_ignored_recommendations extends a_view_model {
    commands: UnignoreRecommendation[] = [];
    outcome!: { total: number; failed: number; failures: string[] };

    constructor() {
        super();
        this.viewModel.showIgnored = true;
        this.viewModel.selectedRecommendations = this.recommendations;
    }

    answerWith(failure: { index: number; message: string } | undefined) {
        const commands = this.commands;
        sinon.stub(UnignoreRecommendation.prototype, 'execute').callsFake(function (this: UnignoreRecommendation) {
            const index = commands.length;
            commands.push(this);
            return Promise.resolve(failure?.index === index ? CommandResult.failed([failure.message]) : CommandResult.empty);
        });
    }

    async unignore() {
        this.outcome = await this.viewModel.unignoreRecommendations([this.firstRecommendationId, this.secondRecommendationId]);
    }
}
