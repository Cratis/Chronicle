// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { Button } from '@cratis/components/Common';
import { Message } from '@cratis/components/Display';
import { Page } from 'Components/Common/Page';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Link, useParams } from 'react-router-dom';
import { CanReindexConstraints } from 'Features/Events/Constraints/Operator/CanReindexConstraints';
import strings from 'Strings';
import { ConstraintsViewModel } from './ConstraintsViewModel';

const messages = strings.eventStore.namespaces.constraints;

export const Constraints = withViewModel(ConstraintsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [authorization] = CanReindexConstraints.use({ eventStore: params.eventStore! });

    return <Page title={messages.title}>
        <div className='flex flex-col items-start gap-4 p-4'>
            <p>{messages.description}</p>
            {viewModel.canReindex(authorization) && <Button
                label={messages.action}
                disabled={viewModel.isReindexing}
                loading={viewModel.isReindexing}
                onClick={() => viewModel.reindex(params.eventStore!, params.namespace!)}
            />}
            <div role='status' aria-live='polite'>
                {viewModel.feedback && <Message severity={viewModel.jobId ? 'success' : 'error'} text={viewModel.feedback} />}
            </div>
            {viewModel.jobId && <Link to='../jobs'>{messages.viewJob.replace('{id}', viewModel.jobId)}</Link>}
        </div>
    </Page>;
});
