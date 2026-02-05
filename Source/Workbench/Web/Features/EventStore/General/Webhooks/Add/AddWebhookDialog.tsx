// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateWebhook } from 'Api/Webhooks';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { Button } from 'primereact/button';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Checkbox } from 'primereact/checkbox';

export const AddWebhookDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();
    const [createWebhook] = CreateWebhook.use();

    const [name, setName] = useState('');
    const [url, setUrl] = useState('');
    const [authType, setAuthType] = useState('None');
    const [basicUsername, setBasicUsername] = useState('');
    const [basicPassword, setBasicPassword] = useState('');
    const [bearerToken, setBearerToken] = useState('');
    const [oauthAuthority, setOauthAuthority] = useState('');
    const [oauthClientId, setOauthClientId] = useState('');
    const [oauthClientSecret, setOauthClientSecret] = useState('');
    const [isActive, setIsActive] = useState(true);
    const [isReplayable, setIsReplayable] = useState(true);

    const authTypes = [
        { label: 'None', value: 'None' },
        { label: 'Basic', value: 'Basic' },
        { label: 'Bearer Token', value: 'Bearer' },
        { label: 'OAuth (Client Credentials)', value: 'OAuth' }
    ];

    const handleSave = async () => {
        if (name && url && params.eventStore) {
            createWebhook.eventStore = params.eventStore;
            createWebhook.name = name;
            createWebhook.url = url;
            createWebhook.authorizationType = authType;
            createWebhook.basicUsername = basicUsername;
            createWebhook.basicPassword = basicPassword;
            createWebhook.bearerToken = bearerToken;
            createWebhook.OAuthAuthority = oauthAuthority;
            createWebhook.OAuthClientId = oauthClientId;
            createWebhook.OAuthClientSecret = oauthClientSecret;
            createWebhook.isActive = isActive;
            createWebhook.isReplayable = isReplayable;
            createWebhook.eventTypes = {};
            createWebhook.headers = {};

            const result = await createWebhook.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    const handleCancel = () => {
        closeDialog(DialogResult.Cancelled);
    };

    return (
        <Dialog
            header="Add Webhook"
            visible={true}
            style={{ width: '600px' }}
            modal
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="p-fluid">
                <div className="field">
                    <label htmlFor="name">Name</label>
                    <InputText id="name" value={name} onChange={(e) => setName(e.target.value)} />
                </div>

                <div className="field">
                    <label htmlFor="url">Webhook URL</label>
                    <InputText id="url" value={url} onChange={(e) => setUrl(e.target.value)} />
                </div>

                <div className="field">
                    <label htmlFor="authType">Authorization Type</label>
                    <Dropdown
                        id="authType"
                        value={authType}
                        options={authTypes}
                        onChange={(e) => setAuthType(e.value)}
                        placeholder="Select authorization type"
                    />
                </div>

                {authType === 'Basic' && (
                    <>
                        <div className="field">
                            <label htmlFor="basicUsername">Username</label>
                            <InputText id="basicUsername" value={basicUsername} onChange={(e) => setBasicUsername(e.target.value)} />
                        </div>
                        <div className="field">
                            <label htmlFor="basicPassword">Password</label>
                            <InputText id="basicPassword" type="password" value={basicPassword} onChange={(e) => setBasicPassword(e.target.value)} />
                        </div>
                    </>
                )}

                {authType === 'Bearer' && (
                    <div className="field">
                        <label htmlFor="bearerToken">Bearer Token</label>
                        <InputText id="bearerToken" value={bearerToken} onChange={(e) => setBearerToken(e.target.value)} />
                    </div>
                )}

                {authType === 'OAuth' && (
                    <>
                        <div className="field">
                            <label htmlFor="oauthAuthority">OAuth Authority URL</label>
                            <InputText id="oauthAuthority" value={oauthAuthority} onChange={(e) => setOauthAuthority(e.target.value)} />
                        </div>
                        <div className="field">
                            <label htmlFor="oauthClientId">Client ID</label>
                            <InputText id="oauthClientId" value={oauthClientId} onChange={(e) => setOauthClientId(e.target.value)} />
                        </div>
                        <div className="field">
                            <label htmlFor="oauthClientSecret">Client Secret</label>
                            <InputText id="oauthClientSecret" type="password" value={oauthClientSecret} onChange={(e) => setOauthClientSecret(e.target.value)} />
                        </div>
                    </>
                )}

                <div className="field-checkbox">
                    <Checkbox inputId="isActive" checked={isActive} onChange={(e) => setIsActive(e.checked || false)} />
                    <label htmlFor="isActive">Active</label>
                </div>

                <div className="field-checkbox">
                    <Checkbox inputId="isReplayable" checked={isReplayable} onChange={(e) => setIsReplayable(e.checked || false)} />
                    <label htmlFor="isReplayable">Replayable</label>
                </div>

                <div className="flex justify-content-end mt-4">
                    <Button label="Cancel" icon="pi pi-times" onClick={handleCancel} className="p-button-text mr-2" />
                    <Button label="Save" icon="pi pi-check" onClick={handleSave} />
                </div>
            </div>
        </Dialog>
    );
};
