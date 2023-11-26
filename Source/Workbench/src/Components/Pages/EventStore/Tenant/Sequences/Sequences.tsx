// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { Queries } from './Queries/Queries';
import { withViewModel } from 'MVVM/withViewModel';
import { Page } from '../../../Page';

export const Sequences = withViewModel(SequencesViewModel, ({ viewModel }) => {
    return (
        <Page title='Event Sequences'>
            <Queries />
        </Page>);
});
