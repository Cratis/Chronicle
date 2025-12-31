// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { AllReadModelDefinitions } from 'Api/ReadModelTypes';
import { Page } from 'Components/Common/Page';
import { JsonSchema } from 'Components/JsonSchema';
import { ProjectionEditor } from 'Components/ProjectionEditor';
import { Menubar } from 'primereact/menubar';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { EventStoreAndNamespaceParams } from 'Shared/EventStoreAndNamespaceParams';
import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Allotment } from 'allotment';
import { AllProjections } from 'Api/Projections';

export const Projections = () => {

    const [dslValue, setDslValue] = useState('');

    /*`Users
| key=UserRegistered.userId
| name=UserRegistered.name
| email=UserRegistered.email
| totalSpent+=OrderCompleted.amount
| orderCount increment by OrderPlaced
| orderCount decrement by OrderCancelled
| lastLogin=$eventContext.occurred
| status="active" on UserRegistered
| orders=[
|    identified by orderId
|    key=OrderPlaced.orderId
|    total=OrderPlaced.total
| ]`);*/

    const params = useParams<EventStoreAndNamespaceParams>();

    const [readModels] = AllReadModelDefinitions.use({ eventStore: params.eventStore! });
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });
    const readModelSchemas = readModels.data?.map(readModel => JSON.parse(readModel.schema) as JsonSchema);
    const eventSchemas = eventTypes.data?.map(eventType => JSON.parse(eventType.schema) as JsonSchema);

    const [projections] = AllProjections.use({ eventStore: params.eventStore! });

    return (
        <Page title='Projections'>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="270px">
                    <div className="px-4 py-4">
                        <DataTable
                            value={projections.data}>

                            <Column field="identifier" header="Identifier" />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                <Allotment.Pane className="h-full">

                    <div className="px-4 py-4">
                        <Menubar
                            model={[
                                {
                                    label: strings.eventStore.general.projections.actions.new,
                                    icon: <faIcons.FaPlus className='mr-2' />
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.save,
                                    icon: <faIcons.FaFloppyDisk className='mr-2' />
                                },
                                {
                                    label: strings.eventStore.general.projections.actions.preview,
                                    icon: <faIcons.FaEye className='mr-2' />
                                }
                            ]}
                        />

                        <div className="py-4">
                            <ProjectionEditor
                                value={dslValue}
                                onChange={setDslValue}
                                readModelSchemas={readModelSchemas}
                                eventSchemas={eventSchemas}
                                height="500px"
                                theme="vs-dark"
                            />
                        </div>

                        <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                            <DataTable
                                value={[]}
                                emptyMessage={strings.eventStore.general.projections.empty}
                                className="p-datatable-sm"
                                pt={{
                                    root: { style: { border: 'none' } },
                                    tbody: { style: { borderTop: '1px solid var(--surface-border)' } }
                                }}>
                                <Column field="name" header="blah" />
                            </DataTable>


                        </div>
                    </div>
                </Allotment.Pane>

            </Allotment>

        </Page>
    );
};
