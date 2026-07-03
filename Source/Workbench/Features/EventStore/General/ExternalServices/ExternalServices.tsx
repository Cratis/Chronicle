// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { GetExternalServices, RemoveExternalService, type ExternalServiceDefinition } from 'Api/ExternalServices';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { AddExternalServiceDialog } from './Add/AddExternalServiceDialog';
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import * as faIcons from 'react-icons/fa6';
import { useState } from 'react';
import { ExternalServiceDetails } from './ExternalServiceDetails';
import { getEndpointTypeString } from './getEndpointTypeString';
import { useDialog } from '@cratis/arc.react/dialogs';

export const ExternalServices = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [selectedExternalService, setSelectedExternalService] = useState<ExternalServiceDefinition | null>(null);
    const [showConfirmation] = useConfirmationDialog();
    const [AddExternalServiceDialogWrapper, showAddExternalServiceDialog] = useDialog(AddExternalServiceDialog);
    const [removeExternalService] = RemoveExternalService.use();
    // TODO: This is a workaround to force refresh after save. Should be replaced with WebSocket-based updates.
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    const handleRemoveExternalService = async () => {
        if (selectedExternalService) {
            const result = await showConfirmation(
                strings.eventStore.general.externalServices.dialogs.removeExternalService.title,
                strings.eventStore.general.externalServices.dialogs.removeExternalService.message.replace('{name}', selectedExternalService.name),
                DialogButtons.YesNo
            );

            if (result === DialogResult.Yes) {
                removeExternalService.eventStore = params.eventStore!;
                removeExternalService.externalServiceId = selectedExternalService.id;
                await removeExternalService.execute();
                setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
            }
        }
    };

    const handleAddExternalService = async () => {
        const [result] = await showAddExternalServiceDialog();
        if (result === DialogResult.Ok) {
            setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
        }
    };

    return (
        <Page title={strings.eventStore.general.externalServices.title}>
            <DataPage
                key={refreshTrigger}
                title={strings.eventStore.general.externalServices.title}
                query={GetExternalServices}
                queryArguments={{ eventStore: params.eventStore! }}
                dataKey='id'
                emptyMessage={strings.eventStore.general.externalServices.empty}
                detailsComponent={ExternalServiceDetails}
                selection={selectedExternalService}
                onSelectionChange={(e) => setSelectedExternalService(e.value as ExternalServiceDefinition)}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.externalServices.actions.add}
                        icon={faIcons.FaPlus}
                        command={handleAddExternalService} />
                    <MenuItem
                        id='remove'
                        label={strings.eventStore.general.externalServices.actions.remove}
                        icon={faIcons.FaTrash}
                        disableOnUnselected
                        command={handleRemoveExternalService} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column
                        style={{ width: '200px' }}
                        field='name'
                        header={strings.eventStore.general.externalServices.columns.name}
                    />
                    <Column
                        field='endpointType'
                        style={{ width: '200px' }}
                        header={strings.eventStore.general.externalServices.columns.endpointType}
                        body={(externalService: ExternalServiceDefinition) => getEndpointTypeString(externalService.endpointType)} />
                    <Column
                        field='url'
                        header={strings.eventStore.general.externalServices.columns.endpoint}
                        body={(externalService: ExternalServiceDefinition) => externalService.url || externalService.host} />
                </DataPage.Columns>
            </DataPage>
            <AddExternalServiceDialogWrapper />
        </Page>
    );
};
