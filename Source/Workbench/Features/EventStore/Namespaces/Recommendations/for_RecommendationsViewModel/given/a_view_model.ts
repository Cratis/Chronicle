// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { RecommendationsViewModel } from '../../RecommendationViewModel';

export class a_view_model {
    constructor() {
        this.firstRecommendationId = Guid.create();
        this.secondRecommendationId = Guid.create();
        this.viewModel = new RecommendationsViewModel({ eventStore: 'the-store', namespace: 'the-namespace' });
    }

    firstRecommendationId: Guid;
    secondRecommendationId: Guid;
    viewModel: RecommendationsViewModel;
}
