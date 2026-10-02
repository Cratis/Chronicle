// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column, DataTableCore } from '@cratis/components/DataTables';
import { Page } from 'Components/Common/Page';
import { EventSourceDetails, ObserveEventSources } from 'Features/EventSources';
import strings from 'Strings';
import { Allotment } from 'allotment';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { type EventStoreParams } from 'Shared';
import { Details } from './Details';

const renderStreams = (eventSource: EventSourceDetails) =>
    eventSource.streams.map(stream => stream.name).join(', ');

export const EventSources = () => {
    const params = useParams<EventStoreParams>();
    const [selectedItem, setSelectedItem] = useState<EventSourceDetails>();
    const [result] = ObserveEventSources.use({ eventStore: params.eventStore! });

    return (
        <Page title={strings.eventStore.general.eventSources.title}>
            <Allotment className='h-full' proportionalLayout={false}>
                <Allotment.Pane className='flex-grow'>
                    <div className='flex flex-col border border-cratis-surface-border rounded mx-4 my-4 overflow-hidden h-[calc(100%-2rem)]'>
                        <DataTableCore
                            data={result.data}
                            scrollable
                            scrollHeight='flex'
                            selectionMode='single'
                            selection={selectedItem}
                            onSelectionChange={event => setSelectedItem(event.value ?? undefined)}
                            dataKey='id'
                            emptyMessage={strings.eventStore.general.eventSources.empty}>
                            <Column field='name' header={strings.eventStore.general.eventSources.columns.name} sortable />
                            <Column field='description' header={strings.eventStore.general.eventSources.columns.description} sortable />
                            <Column header={strings.eventStore.general.eventSources.columns.streams} body={renderStreams} />
                        </DataTableCore>
                    </div>
                </Allotment.Pane>
                {selectedItem && (
                    <Allotment.Pane preferredSize='450px'>
                        <Details item={selectedItem} />
                    </Allotment.Pane>
                )}
            </Allotment>
        </Page>
    );
};
