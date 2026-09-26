// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RecommendationDetails } from 'Features/Recommendations';
import { inject, injectable } from 'tsyringe';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { IgnoreRecommendation, PerformRecommendation } from 'Features/Recommendations';
import { Guid } from '@cratis/fundamentals';

/** The result of applying an action to a set of recommendations. */
export interface RecommendationActionOutcome {
    /** How many recommendations were attempted. */
    total: number;

    /** How many of them failed. */
    failed: number;

    /** One message per failure, identifying the recommendation and what went wrong. */
    failures: string[];
}

@injectable()
export class RecommendationsViewModel {

    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {
    }

    /**
     * The recommendations the user has selected.
     *
     * There is one selection, and every action reads it. The page previously carried two - the data
     * table's own single-row selection driving Perform, and a separate checkbox column driving Ignore -
     * so whichever way the user selected, one of the two actions stayed greyed out.
     */
    selectedRecommendations: RecommendationDetails[] = [];

    clearRecommendationSelection() {
        this.selectedRecommendations = [];
    }

    /**
     * Performs every recommendation in the given selection.
     * @param recommendationIds The identifiers of the recommendations to perform.
     * @returns What happened, so the caller can tell the user.
     */
    performRecommendations(recommendationIds: Guid[]): Promise<RecommendationActionOutcome> {
        return this.applyToEach(recommendationIds, recommendationId => {
            const command = new PerformRecommendation();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.recommendationId = recommendationId;
            return command;
        });
    }

    /**
     * Ignores every recommendation in the given selection. The identifiers are passed in explicitly
     * rather than read from {@link selectedRecommendations} at call time, because the caller captures
     * them before awaiting a confirmation dialog - by the time the user responds, the live selection
     * could have moved on.
     * @param recommendationIds The identifiers of the recommendations to ignore.
     * @returns What happened, so the caller can tell the user.
     */
    ignoreRecommendations(recommendationIds: Guid[]): Promise<RecommendationActionOutcome> {
        return this.applyToEach(recommendationIds, recommendationId => {
            const command = new IgnoreRecommendation();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.recommendationId = recommendationId;
            return command;
        });
    }

    /**
     * Runs a command for each identifier, collecting failures rather than stopping at the first, so
     * one failure among many neither hides the others nor hides itself.
     */
    private async applyToEach(
        recommendationIds: Guid[],
        build: (recommendationId: Guid) => { execute(): Promise<{ onFailed(callback: () => void): void; validationResults: { message: string }[]; exceptionMessages: string[] }> }
    ): Promise<RecommendationActionOutcome> {
        const failures: string[] = [];

        for (const recommendationId of recommendationIds) {
            // eslint-disable-next-line no-await-in-loop
            const result = await build(recommendationId).execute();
            result.onFailed(() => {
                const messages = [...result.validationResults.map(_ => _.message), ...result.exceptionMessages];
                failures.push(`${recommendationId.toString()}: ${messages.join(', ') || 'Unknown error'}`);
            });
        }

        this.clearRecommendationSelection();

        return { total: recommendationIds.length, failed: failures.length, failures };
    }
}
