// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { DataPage, MenuItem } from 'Components';
import { AllObservers, AllObserversArguments } from 'Api/Observation';
import { Column } from 'primereact/column';
import * as faIcons from 'react-icons/fa6';
import { ObserverInformation, ObserverType } from 'Api/Concepts/Observation';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

const observerType = (observer: ObserverInformation) => {
    switch (observer.type) {
        case ObserverType.unknown:
            return 'Unknown';
        case ObserverType.client:
            return 'Client';
        case ObserverType.projection:
            return 'Projection';
        case ObserverType.reducer:
            return 'Reducer';
    }
    return '[N/A]';
};


export const Jobs = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AllObserversArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.observers.title}
            query={AllObservers}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.observers.empty}
            dataKey='observerId'>
            <DataPage.MenuItems>
                <MenuItem
                    id="replay"
                    label={strings.eventStore.namespaces.observers.actions.replay} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => { }} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field='observerId' header={strings.eventStore.namespaces.observers.columns.id} sortable />
                <Column
                    field='type'
                    header={strings.eventStore.namespaces.observers.columns.observerType}
                    sortable
                    body={observerType} />

                <Column
                    field='nextEventSequenceNumber'
                    dataType='numeric'
                    header={strings.eventStore.namespaces.observers.columns.nextEventSequenceNumber}
                    sortable />
            </DataPage.Columns>
        </DataPage>
    );
};
