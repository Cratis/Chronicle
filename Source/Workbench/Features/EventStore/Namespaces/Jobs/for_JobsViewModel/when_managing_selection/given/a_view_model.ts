// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { JobsViewModel } from '../../../JobsViewModel';

export class a_view_model {
    constructor() {
        this.firstJobId = Guid.parse('11111111-1111-1111-1111-111111111111');
        this.secondJobId = Guid.parse('22222222-2222-2222-2222-222222222222');
        this.viewModel = new JobsViewModel({ eventStore: 'some-event-store', namespace: 'some-namespace' });
    }

    firstJobId: Guid;
    secondJobId: Guid;
    viewModel: JobsViewModel;
}
