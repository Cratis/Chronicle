// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column, DataTableCore } from '@cratis/components/DataTables';
import type { IDetailsComponentProps } from '@cratis/components/DataPage';
import { EventStreamDefinition } from 'Features/Contracts/EventSources';
import { EventSourceDetails } from 'Features/EventSources';
import strings from 'Strings';
import { formatConcurrency } from './Formatting';

const renderConcurrency = (stream: EventStreamDefinition) => formatConcurrency(stream.concurrency);

export const Details = ({ item }: IDetailsComponentProps<EventSourceDetails>) => (
    <div className='flex flex-col h-full p-4 gap-4'>
        <div>
            <h2 className='m-0'>{item.name}</h2>
            <p className='m-0 mt-2 text-color-secondary'>{item.description}</p>
        </div>
        <DataTableCore
            data={item.streams}
            dataKey='name'
            emptyMessage={strings.eventStore.general.eventSources.streams.empty}>
            <Column field='name' header={strings.eventStore.general.eventSources.streams.columns.name} />
            <Column field='description' header={strings.eventStore.general.eventSources.streams.columns.description} />
            <Column header={strings.eventStore.general.eventSources.streams.columns.concurrency} body={renderConcurrency} />
        </DataTableCore>
    </div>
);
