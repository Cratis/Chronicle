// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { RecommendationsViewModel } from '../../../RecommendationViewModel';

export class a_view_model {
    constructor() {
        this.firstRecommendationId = Guid.parse('11111111-1111-1111-1111-111111111111');
        this.secondRecommendationId = Guid.parse('22222222-2222-2222-2222-222222222222');
        this.viewModel = new RecommendationsViewModel({ eventStore: 'some-event-store', namespace: 'some-namespace' });
    }

    firstRecommendationId: Guid;
    secondRecommendationId: Guid;
    viewModel: RecommendationsViewModel;
}
