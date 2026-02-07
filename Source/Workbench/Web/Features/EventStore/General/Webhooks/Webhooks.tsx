// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { GetWebhooks, type WebhookDefinition } from 'Api/Webhooks';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useDialog, DialogResult } from '@cratis/arc.react/dialogs';
import { AddWebhookDialog } from './Add/AddWebhookDialog';
import { DataPage, MenuItem } from 'Components';
import { WebhookDetails } from './WebhookDetails';
import * as faIcons from 'react-icons/fa6';
import { useState } from 'react';

const renderAuthorizationType = (webhook: WebhookDefinition) => {
    const authType = webhook.authorizationType?.toLowerCase() || 'none';
    return strings.eventStore.general.webhooks.authTypes[authType as keyof typeof strings.eventStore.general.webhooks.authTypes] || webhook.authorizationType || 'None';
};

const renderBoolean = (value: boolean) => {
    return value ? 'Yes' : 'No';
};

export const Webhooks = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddWebhookWrapper, showAddWebhook] = useDialog(AddWebhookDialog);
    // TODO: This is a workaround to force refresh after save. Should be replaced with WebSocket-based updates.
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    const handleAddWebhook = async () => {
        const [result] = await showAddWebhook();
        if (result === DialogResult.Ok) {
            setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
        }
    };

    return (
        <>
            <DataPage
                key={refreshTrigger}
                title={strings.eventStore.general.webhooks.title}
                query={GetWebhooks}
                queryArguments={{ eventStore: params.eventStore! }}
                dataKey='id'
                emptyMessage={strings.eventStore.general.webhooks.empty}
                detailsComponent={WebhookDetails}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.webhooks.actions.add}
                        icon={faIcons.FaPlus}
                        command={handleAddWebhook} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column
                        style={{ width: '200px' }}
                        field='name' header={strings.eventStore.general.webhooks.columns.name}
                        />
                    <Column
                        field='url'
                        header={strings.eventStore.general.webhooks.columns.url} />
                    <Column
                        field='authorizationType'
                        style={{ width: '150px' }}
                        header={strings.eventStore.general.webhooks.columns.authorization}
                        body={renderAuthorizationType} />
                    <Column
                        field='isActive'
                        style={{ width: '80px' }}
                        header={strings.eventStore.general.webhooks.columns.active}
                        body={(rowData: WebhookDefinition) => renderBoolean(rowData.isActive)} />
                    <Column
                        field='isReplayable'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.webhooks.columns.replayable}
                        body={(rowData: WebhookDefinition) => renderBoolean(rowData.isReplayable)} />
                </DataPage.Columns>
            </DataPage>
            <AddWebhookWrapper />
        </>
    );
};
