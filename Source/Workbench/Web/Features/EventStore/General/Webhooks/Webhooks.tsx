// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataTable } from '@cratis/arc.react/primereact';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { GetWebhooks, type WebhookDefinition } from 'Api/Webhooks';
import { Column } from 'primereact/column';
import { Button } from '@cratis/arc.react/button';
import { useDialog } from '@cratis/arc.react/dialogs';
import strings from 'Strings';
import { AddWebhookDialog } from './Add/AddWebhookDialog';

export const Webhooks = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [getWebhooks] = GetWebhooks.use();
    const [AddWebhookWrapper, showAddWebhook] = useDialog(AddWebhookDialog);

    getWebhooks.eventStore = params.eventStore!;

    const handleAddWebhook = async () => {
        const [result] = await showAddWebhook();
        if (result) {
            await getWebhooks.execute();
        }
    };

    return (
        <>
            <div className="flex justify-content-between align-items-center mb-3">
                <h2>Webhooks</h2>
                <Button
                    label="Add Webhook"
                    icon="pi pi-plus"
                    command={handleAddWebhook} />
            </div>

            <DataTable
                value={getWebhooks.data}
                loading={getWebhooks.isLoading}
                emptyMessage="No webhooks configured">
                <Column field="name" header="Name" />
                <Column field="url" header="URL" />
                <Column
                    field="authorizationType"
                    header="Authorization"
                    body={(rowData: WebhookDefinition) => rowData.authorizationType || 'None'} />
                <Column
                    field="isActive"
                    header="Active"
                    body={(rowData: WebhookDefinition) => (rowData.isActive ? 'Yes' : 'No')} />
                <Column
                    field="isReplayable"
                    header="Replayable"
                    body={(rowData: WebhookDefinition) => (rowData.isReplayable ? 'Yes' : 'No')} />
            </DataTable>

            <AddWebhookWrapper />
        </>
    );
};
