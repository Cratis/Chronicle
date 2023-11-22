import { QueryTabViewModel } from './Query/QueryTabViewModel';
import { SequencesViewModel } from './SequencesViewModel';
import { QueryTabs } from './Query/QueryTabs';
import { observer } from 'mobx-react';
import { container } from 'tsyringe';
export interface SequencesProps {
    viewModel: SequencesViewModel;
}

export const Sequences = observer(() => {
    return (
        <div className='p-4'>
            <h1 className='text-3xl m-3'> Queries</h1>
            <QueryTabs viewModel={container.resolve(QueryTabViewModel)} />
        </div>
    );
});
