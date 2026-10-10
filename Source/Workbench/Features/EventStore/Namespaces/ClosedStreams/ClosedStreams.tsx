// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { Column, DataPage } from '@cratis/components/DataPage';
import { useParams } from 'react-router-dom';
import { ClosedStreams as ClosedStreamsQuery, type ClosedStreamsParameters } from 'Features/Sequences/ClosedStreams';
import { type ClosedStream } from 'Features/Sequences/ClosedStream';
import { ClosedStreamOrigin } from 'Features/Sequences/ClosedStreamOrigin';
import { Page } from 'Components/Common/Page';

export const ClosedStreams = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: ClosedStreamsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };
    const labels = strings.eventStore.namespaces.closedStreams;

    return (
        <Page title={labels.title}>
            <DataPage
                title={labels.title}
                query={ClosedStreamsQuery}
                queryArguments={queryArgs}
                emptyMessage={labels.empty}>
                <DataPage.Columns>
                    <Column field='eventSourceId' header={labels.columns.eventSourceId} />
                    <Column field='eventSourceType' header={labels.columns.eventSourceType} />
                    <Column field='eventStreamType' header={labels.columns.eventStreamType} />
                    <Column field='eventStreamId' header={labels.columns.eventStreamId} />
                    <Column<ClosedStream> field='origin' header={labels.columns.origin} body={item => item.origin === ClosedStreamOrigin.completeStream ? labels.origins.manual : labels.origins.event} />
                    <Column field='closedBy' header={labels.columns.closedBy} />
                    <Column<ClosedStream> field='sequenceNumber' header={labels.columns.sequenceNumber} body={item => item.sequenceNumber.toString()} />
                    <Column<ClosedStream> field='closedAt' header={labels.columns.closedAt} body={item => item.closedAt?.toLocaleString() ?? ''} />
                </DataPage.Columns>
            </DataPage>
        </Page>
    );
};
