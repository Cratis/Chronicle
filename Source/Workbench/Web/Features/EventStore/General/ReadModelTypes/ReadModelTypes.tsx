// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { AllReadModelDefinitions, ReadModelDefinition, ReadModelOwner, ReadModelSource } from 'Api/ReadModelTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useDialog, DialogResult } from '@cratis/arc.react/dialogs';
import { AddReadModelDialog } from './Add/AddReadModelDialog';
import { DataPage, MenuItem } from 'Components';
import { ReadModelDetails } from './ReadModelDetails';
import * as faIcons from 'react-icons/fa6';
import { useState } from 'react';

const renderSource = (readModel: ReadModelDefinition) => {
    switch (readModel.source) {
        case ReadModelSource.code:
            return strings.eventStore.general.readModels.sources.code;
        case ReadModelSource.user:
            return strings.eventStore.general.readModels.sources.user;
    }
    return strings.eventStore.general.readModels.sources.unknown;
};

const renderOwner = (readModel: ReadModelDefinition) => {
    switch (readModel.owner) {
        case ReadModelOwner.client:
            return strings.eventStore.general.readModels.owners.client;
        case ReadModelOwner.server:
            return strings.eventStore.general.readModels.owners.server;
    }
    return strings.eventStore.general.readModels.owners.unknown;
};

export const ReadModelTypes = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddReadModelWrapper, showAddReadModel] = useDialog(AddReadModelDialog);
    // TODO: This is a workaround to force refresh after save. Should be replaced with WebSocket-based updates.
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    const handleAddReadModel = async () => {
        const [result] = await showAddReadModel();
        if (result === DialogResult.Ok) {
            setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
        }
    };

    return (
        <>
            <DataPage
                key={refreshTrigger}
                title={strings.eventStore.general.readModels.title}
                query={AllReadModelDefinitions}
                queryArguments={{ eventStore: params.eventStore! }}
                dataKey='identifier'
                emptyMessage={strings.eventStore.general.readModels.empty}
                detailsComponent={ReadModelDetails}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.readModels.actions.create}
                        icon={faIcons.FaPlus}
                        command={handleAddReadModel} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column
                        style={{ width: '300px' }}
                        field='displayName' header={strings.eventStore.general.readModels.columns.name}
                        />
                    <Column
                        field='name'
                        header={strings.eventStore.general.readModels.columns.containerName} />
                    <Column
                        field='owner'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.owner}
                        body={renderOwner} />
                    <Column
                        field='source'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.source}
                        body={renderSource} />
                    <Column
                        field='generation'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.generation} />
                </DataPage.Columns>
            </DataPage>
            <AddReadModelWrapper />
        </>
    );
};
