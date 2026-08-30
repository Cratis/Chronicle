// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RecommendationDetails } from 'Features/Recommendations';
import { inject, injectable } from 'tsyringe';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { IgnoreRecommendation, PerformRecommendation } from 'Features/Recommendations';

@injectable()
export class RecommendationsViewModel {

    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {
    }

    selectedRecommendation: RecommendationDetails | undefined;

    async perform() {
        if (this.selectedRecommendation) {
            const command = new PerformRecommendation();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.recommendationId = this.selectedRecommendation.id;
            await command.execute();
        }
    }

    async ignore() {
        if (this.selectedRecommendation) {
            const command = new IgnoreRecommendation();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.recommendationId = this.selectedRecommendation.id;
            await command.execute();
        }
    }
}
