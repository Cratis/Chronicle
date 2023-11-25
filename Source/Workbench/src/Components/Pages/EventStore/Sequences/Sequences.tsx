// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { Queries } from './Queries/Queries';
import { withViewModel } from 'MVVM/withViewModel';

export const Sequences = withViewModel(SequencesViewModel, ({viewModel}) => {
    return (
        <div className='p-4'>
            <h1 className='text-3xl m-3'>Queries</h1>
            <Queries />
        </div>
    );
});
