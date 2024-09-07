// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RecommendationInformation } from 'Api/Concepts/Recommendations';
import { injectable } from 'tsyringe';

@injectable()
export class RecommendationsViewModel {

    selectedRecommendation: RecommendationInformation | undefined;

    async reject() {
        if (this.selectedRecommendation) {
            console.log('Reject');
        }
    }
}
