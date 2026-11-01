// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link, useParams, useSearchParams } from 'react-router-dom';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { EventDetails } from './EventDetails';
import { SequenceEventDetailsProps, SequenceEventDetailsViewModel } from './SequenceEventDetailsViewModel';
import { sequenceIdFromSearch } from './sequenceEventPath';

export const SequenceEventDetails = () => {
    const params = useParams();
    const [search] = useSearchParams();
    const origins = search.getAll('failedPartitionId');
    return <SequenceEventDetailsView eventStore={params.eventStore!} namespace={params.namespace!}
        sequenceNumber={params.sequenceNumber} eventSequenceId={sequenceIdFromSearch(search)}
        failedPartitionId={origins.length === 1 ? origins[0] : undefined} />;
};

const SequenceEventDetailsView = withViewModel<SequenceEventDetailsViewModel, SequenceEventDetailsProps>(SequenceEventDetailsViewModel, ({ viewModel, props }) => {
    const text = strings.eventStore.namespaces.sequences.inspection;
    return (
        <Page title={text.title}>
            <div className='overflow-auto p-4'>
                <Link to={viewModel.returnPath}>{props.failedPartitionId ? text.return : text.returnToSequences}</Link>
                <h2>{text.title}</h2>
                <dl>
                    <dt>{text.eventStore}</dt><dd>{props.eventStore}</dd>
                    <dt>{text.namespace}</dt><dd>{props.namespace}</dd>
                    <dt>{text.sequence}</dt><dd>{props.eventSequenceId}</dd>
                    <dt>{text.number}</dt><dd>{props.sequenceNumber}</dd>
                </dl>
                {viewModel.state !== 'ready' && <p role='status'>{text[viewModel.state]}</p>}
                {(viewModel.state === 'failed' || viewModel.state === 'denied') && (
                    <button type='button' onClick={() => viewModel.retry()}>{text.retry}</button>
                )}
                {viewModel.event && <EventDetails key={JSON.stringify([props.eventStore, props.namespace, props.eventSequenceId, props.sequenceNumber])} item={viewModel.event} />}
            </div>
        </Page>
    );
});
