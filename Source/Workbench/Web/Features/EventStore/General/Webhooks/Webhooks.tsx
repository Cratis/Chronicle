// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { GetWebhooks, RemoveWebHook, type WebhookDefinition } from 'Api/Webhooks';
import { AuthorizationType } from 'Api/Security';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useDialog, useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { AddWebhookDialog } from './Add/AddWebhookDialog';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';
import { useState } from 'react';
import { WebhookDetails } from './WebhookDetails';

const renderAuthorizationType = (webhook: WebhookDefinition) => {
    switch (webhook.authorizationType) {
        case AuthorizationType.none:
            return strings.eventStore.general.webhooks.authTypes.none;
        case AuthorizationType.basic:
            return strings.eventStore.general.webhooks.authTypes.basic;
        case AuthorizationType.bearer:
            return strings.eventStore.general.webhooks.authTypes.bearer;
        case AuthorizationType.OAuth:
            return strings.eventStore.general.webhooks.authTypes.oauth;
        default:
            return strings.eventStore.general.webhooks.authTypes.none;
    }
};

const renderBoolean = (value: boolean) => {
    return value ? 'Yes' : 'No';
};

export const Webhooks = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [selectedWebhook, setSelectedWebhook] = useState<WebhookDefinition | null>(null);
    const [AddWebhookWrapper, showAddWebhook] = useDialog(AddWebhookDialog);
    const [showConfirmation] = useConfirmationDialog();
    const [removeWebhook] = RemoveWebHook.use();
    // TODO: This is a workaround to force refresh after save. Should be replaced with WebSocket-based updates.
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    const handleAddWebhook = async () => {
        const [result] = await showAddWebhook();
        if (result === DialogResult.Ok) {
            setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
        }
    };

    const handleRemoveWebhook = async () => {
        if (selectedWebhook) {
            const result = await showConfirmation(
                strings.eventStore.general.webhooks.dialogs.removeWebhook.title,
                strings.eventStore.general.webhooks.dialogs.removeWebhook.message.replace('{name}', selectedWebhook.name),
                DialogButtons.YesNo
            );

            if (result === DialogResult.Yes) {
                removeWebhook.eventStore = params.eventStore!;
                removeWebhook.webhookId = selectedWebhook.id;
                await removeWebhook.execute();
                setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
            }
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
                detailsComponent={WebhookDetails}
                selection={selectedWebhook}
                onSelectionChange={(e) => setSelectedWebhook(e.value as WebhookDefinition)}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.webhooks.actions.add}
                        icon={faIcons.FaPlus}
                        command={handleAddWebhook} />
                    <MenuItem
                        id='remove'
                        label={strings.eventStore.general.webhooks.actions.remove}
                        icon={faIcons.FaTrash}
                        disableOnUnselected
                        command={handleRemoveWebhook} />
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

