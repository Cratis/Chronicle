// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllReadModelDefinitions, ReadModelDefinition, ReadModelOwner, ReadModelSource } from 'Api/ReadModelTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { AddReadModelDialog } from './Add/AddReadModelDialog';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import { ReadModelDetails } from './ReadModelDetails';
import * as faIcons from 'react-icons/fa6';
import { useState, useCallback } from 'react';
import { type DataTableFilterMeta } from '@cratis/components/DataTables';
import { FilterMatchMode } from '@primereact/headless/datatable';
import { useDialog, DialogResult } from '@cratis/arc.react/dialogs';

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
    const [AddReadModelDialogWrapper, showAddReadModelDialog] = useDialog(AddReadModelDialog);
    // TODO: This is a workaround to force refresh after save. Should be replaced with WebSocket-based updates.
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    const handleRefresh = useCallback(() => {
        setRefreshTrigger(prev => prev + 1);
    }, []);

    const handleAddReadModel = async () => {
        const [result] = await showAddReadModelDialog();
        if (result === DialogResult.Ok) {
            handleRefresh();
        }
    };

    const filters: DataTableFilterMeta = {
        owner: { value: null, matchMode: FilterMatchMode.Equals },
        source: { value: null, matchMode: FilterMatchMode.Equals }
    };

    return (
        <Page title={strings.eventStore.general.readModels.title}>
            <DataPage
                key={refreshTrigger}
                title={strings.eventStore.general.readModels.title}
                query={AllReadModelDefinitions}
                queryArguments={{ eventStore: params.eventStore! }}
                dataKey='identifier'
                emptyMessage={strings.eventStore.general.readModels.empty}
                defaultFilters={filters}
                clientFiltering
                onRefresh={handleRefresh}
                detailsComponent={ReadModelDetails}>

                <DataPage.MenuItems>
                    <MenuItem
                        label={strings.eventStore.general.readModels.actions.create}
                        icon={faIcons.FaPlus}
                        command={handleAddReadModel} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column
                        style={{ width: '300px' }}
                        field='identifier' header={strings.eventStore.general.readModels.columns.fullName}
                        />
                    <Column
                        style={{ width: '200px' }}
                        field='displayName' header={strings.eventStore.general.readModels.columns.name}
                        />
                    <Column
                        field='containerName'
                        header={strings.eventStore.general.readModels.columns.containerName} />
                    <Column
                        field='owner'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.owner}
                        showFilterMatchModes={false}
                        filter
                        filterField='owner'
                        body={renderOwner} />
                    <Column
                        field='source'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.source}
                        showFilterMatchModes={false}
                        filter
                        filterField='source'
                        body={renderSource} />
                    <Column
                        field='generation'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.generation} />
                </DataPage.Columns>
            </DataPage>
            <AddReadModelDialogWrapper />
        </Page>
    );
};
