import { QueriesViewModel } from './Queries/QueriesViewModel';
import { SequencesViewModel } from './SequencesViewModel';
import { Queries } from './Queries/Queries';
import { observer } from 'mobx-react';
import { container } from 'tsyringe';
export interface SequencesProps {
    viewModel: SequencesViewModel;
}

export const Sequences = observer(() => {
    return (
        <div className='p-4'>
            <h1 className='text-3xl m-3'> Queries</h1>
            <Queries viewModel={container.resolve(QueriesViewModel)} />
        </div>
    );
});
