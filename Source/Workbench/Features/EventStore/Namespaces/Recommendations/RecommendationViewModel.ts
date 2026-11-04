// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RecommendationDetails, IgnoreRecommendation, PerformRecommendation, UnignoreRecommendation } from 'Features/Recommendations';
import { inject, injectable } from 'tsyringe';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Guid } from '@cratis/fundamentals';
import strings from 'Strings';

@injectable()
export class RecommendationsViewModel {

    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {
    }

    selectedRecommendations: RecommendationDetails[] = [];
    isBusy = false;
    showIgnored = false;

    toggleIgnoredView() {
        this.showIgnored = !this.showIgnored;
        this.selectedRecommendations = [];
    }

    reconcileSelection(recommendations: RecommendationDetails[]) {
        this.selectedRecommendations = this.selectedRecommendations.filter(selected =>
            recommendations.some(recommendation => recommendation.id.equals(selected.id)));
    }

    performRecommendations(recommendationIds: Guid[]) {
        return this.applyToEach([...recommendationIds], PerformRecommendation);
    }

    ignoreRecommendations(recommendationIds: Guid[]) {
        return this.applyToEach([...recommendationIds], IgnoreRecommendation);
    }

    unignoreRecommendations(recommendationIds: Guid[]) {
        return this.applyToEach([...recommendationIds], UnignoreRecommendation);
    }

    private async applyToEach(recommendationIds: Guid[], Command: typeof PerformRecommendation | typeof IgnoreRecommendation | typeof UnignoreRecommendation)
        : Promise<{ total: number; failed: number; failures: string[] }> {
        const failures: string[] = [];
        for (const recommendationId of recommendationIds) {
            try {
                const command = new Command();
                command.eventStore = this._params.eventStore!;
                command.namespace = this._params.namespace!;
                command.recommendationId = recommendationId;
                const result = await command.execute();
                if (!result.isSuccess) {
                    const messages = [...result.validationResults.map(validation => validation.message), ...result.exceptionMessages];
                    failures.push(`${recommendationId}: ${messages.join(', ') || result.authorizationFailureReason || strings.eventStore.namespaces.recommendations.notifications.unknownError}`);
                } else {
                    // Remove only successful targets, never a newer unrelated selection or a failed target.
                    this.selectedRecommendations = this.selectedRecommendations.filter(selected => !selected.id.equals(recommendationId));
                }
            } catch (error) {
                failures.push(`${recommendationId}: ${error instanceof Error ? error.message : strings.eventStore.namespaces.recommendations.notifications.unknownError}`);
            }
        }
        return { total: recommendationIds.length, failed: failures.length, failures };
    }
}
