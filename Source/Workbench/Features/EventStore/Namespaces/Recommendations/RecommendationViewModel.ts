// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RecommendationDetails } from 'Features/Recommendations';
import { inject, injectable } from 'tsyringe';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { IgnoreRecommendation, PerformRecommendation } from 'Features/Recommendations';
import { Guid } from '@cratis/fundamentals';

@injectable()
export class RecommendationsViewModel {

    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {
    }

    selectedRecommendation: RecommendationDetails | undefined;
    selectedRecommendationIds: Guid[] = [];

    isRecommendationSelected(id: Guid): boolean {
        return this.selectedRecommendationIds.some(_ => _.equals(id));
    }

    toggleRecommendationSelection(id: Guid) {
        this.selectedRecommendationIds = this.isRecommendationSelected(id)
            ? this.selectedRecommendationIds.filter(_ => !_.equals(id))
            : [...this.selectedRecommendationIds, id];
    }

    selectAllRecommendations(ids: Guid[]) {
        this.selectedRecommendationIds = [...ids];
    }

    clearRecommendationSelection() {
        this.selectedRecommendationIds = [];
    }

    async perform() {
        if (this.selectedRecommendation) {
            const command = new PerformRecommendation();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.recommendationId = this.selectedRecommendation.id;
            await command.execute();
        }
    }

    /**
     * Ignores every recommendation in the given selection. The identifiers are passed in explicitly
     * rather than read from {@link selectedRecommendation}/{@link selectedRecommendationIds} at call
     * time, because the caller captures them before awaiting a confirmation dialog - by the time the
     * user responds, the live selection could have moved on. Partial failures are collected and thrown
     * as a single aggregate error once every ignore has been attempted, so one failure among many does
     * not swallow the others - and does not swallow itself either.
     * @param recommendationIds The identifiers of the recommendations to ignore.
     */
    async ignoreRecommendations(recommendationIds: Guid[]) {
        const failures: string[] = [];

        for (const recommendationId of recommendationIds) {
            const command = new IgnoreRecommendation();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.recommendationId = recommendationId;

            // eslint-disable-next-line no-await-in-loop
            const result = await command.execute();
            result.onFailed(() => {
                const messages = [...result.validationResults.map(_ => _.message), ...result.exceptionMessages];
                failures.push(`${recommendationId.toString()}: ${messages.join(', ') || 'Unknown error'}`);
            });
        }

        this.clearRecommendationSelection();

        if (failures.length > 0) {
            throw new Error(`Failed to ignore ${failures.length} of ${recommendationIds.length} recommendation(s):\n${failures.join('\n')}`);
        }
    }
}
