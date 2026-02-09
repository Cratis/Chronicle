// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { AddWebHook } from 'Api/Webhooks';
import { AllEventSequences } from 'Api/EventSequences';
import { AllEventTypes } from 'Api/EventTypes';
import { EventType } from 'Api/Events';
import { AuthorizationType } from 'Api/Security';
import { Dialog } from 'Components/Dialogs';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { MultiSelect } from 'primereact/multiselect';
import { InputSwitch } from 'primereact/inputswitch';
import { Message } from 'primereact/message';
import { useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export const AddWebhookDialog = () => {
    const { closeDialog } = useDialogContext();
    const params = useParams<EventStoreAndNamespaceParams>();
    const [addWebhook] = AddWebHook.use();

    const [allEventSequences] = AllEventSequences.use({ eventStore: params.eventStore! });
    const [allEventTypes] = AllEventTypes.use({ eventStore: params.eventStore! });

    const [name, setName] = useState('');
    const [url, setUrl] = useState('');
    const [eventSequence, setEventSequence] = useState('event-log');
    const [selectedEventTypes, setSelectedEventTypes] = useState<EventType[]>([]);
    const [authType, setAuthType] = useState(AuthorizationType.none);
    const [basicUsername, setBasicUsername] = useState('');
    const [basicPassword, setBasicPassword] = useState('');
    const [bearerToken, setBearerToken] = useState('');
    const [oauthAuthority, setOauthAuthority] = useState('');
    const [oauthClientId, setOauthClientId] = useState('');
    const [oauthClientSecret, setOauthClientSecret] = useState('');
    const [isActive, setIsActive] = useState(true);
    const [isReplayable, setIsReplayable] = useState(true);
    const [validationErrors, setValidationErrors] = useState<string[]>([]);

    const authTypes = [
        { label: strings.eventStore.general.webhooks.authTypes.none, value: AuthorizationType.none },
        { label: strings.eventStore.general.webhooks.authTypes.basic, value: AuthorizationType.basic },
        { label: strings.eventStore.general.webhooks.authTypes.bearer, value: AuthorizationType.bearer },
        { label: strings.eventStore.general.webhooks.authTypes.oauth, value: AuthorizationType.OAuth }
    ];

    const eventSequenceOptions = allEventSequences.data.map(seq => ({ label: seq, value: seq }));

    const isUrlValid = (urlString: string): boolean => {
        try {
            new URL(urlString);
            return true;
        } catch {
            return false;
        }
    };

    const isValid = name.trim() !== '' &&
                    url.trim() !== '' &&
                    isUrlValid(url) &&
                    eventSequence.trim() !== '' &&
                    selectedEventTypes.length > 0;

    const extractErrors = (result: { isValid: boolean; validationResults: { message: string }[]; hasExceptions: boolean; exceptionMessages: string[]; isAuthorized: boolean; authorizationFailureReason: string }) => {
        const errors: string[] = [];

        if (!result.isValid) {
            errors.push(...result.validationResults.map(vr => vr.message));
        }

        if (result.hasExceptions) {
            errors.push(...result.exceptionMessages);
        }

        if (!result.isAuthorized && result.authorizationFailureReason) {
            errors.push(result.authorizationFailureReason);
        }

        if (errors.length === 0) {
            errors.push('An unexpected error occurred while adding the webhook.');
        }

        return errors;
    };

    const handleSave = async () => {
        if (name && url && eventSequence && params.eventStore) {
            setValidationErrors([]);

            addWebhook.eventStore = params.eventStore;
            addWebhook.name = name;
            addWebhook.url = url;
            addWebhook.eventSequenceId = eventSequence;
            addWebhook.eventTypes = selectedEventTypes;
            addWebhook.authorizationType = authType;
            addWebhook.basicUsername = basicUsername;
            addWebhook.basicPassword = basicPassword;
            addWebhook.bearerToken = bearerToken;
            addWebhook.OAuthAuthority = oauthAuthority;
            addWebhook.OAuthClientId = oauthClientId;
            addWebhook.OAuthClientSecret = oauthClientSecret;
            addWebhook.isActive = isActive;
            addWebhook.isReplayable = isReplayable;
            addWebhook.headers = {};

            const validationResult = await addWebhook.validate();
            if (!validationResult.isSuccess) {
                setValidationErrors(extractErrors(validationResult));
                return;
            }

            const executeResult = await addWebhook.execute();
            if (executeResult.isSuccess) {
                closeDialog(DialogResult.Ok);
            } else {
                setValidationErrors(extractErrors(executeResult));
            }
        }
    };

    const customButtons = (
        <>
            <Button
                label={strings.general.buttons.ok}
                icon="pi pi-check"
                onClick={handleSave}
                disabled={!isValid}
                autoFocus
            />
            <Button
                label={strings.general.buttons.cancel}
                icon="pi pi-times"
                onClick={() => closeDialog(DialogResult.Cancelled)}
                outlined
            />
        </>
    );

    return (
        <Dialog
            title={strings.eventStore.general.webhooks.dialogs.addWebhook.title}
            // eslint-disable-next-line @typescript-eslint/no-empty-function
            onClose={() => { }}
            buttons={customButtons}
            width="600px"
            resizable={true}
            isValid={isValid}>
            <div className="p-fluid">
                <div className="field mb-3">
                    <label htmlFor="name">{strings.eventStore.general.webhooks.dialogs.addWebhook.name}</label>
                    <InputText id="name" value={name} onChange={(e) => setName(e.target.value)} />
                </div>

                <div className="field mb-3">
                    <label htmlFor="url">{strings.eventStore.general.webhooks.dialogs.addWebhook.url}</label>
                    <InputText id="url" value={url} onChange={(e) => setUrl(e.target.value)} />
                </div>

                <div className="field mb-3">
                    <label htmlFor="eventSequence">{strings.eventStore.general.webhooks.dialogs.addWebhook.eventSequence}</label>
                    <Dropdown
                        id="eventSequence"
                        value={eventSequence}
                        options={eventSequenceOptions}
                        onChange={(e) => setEventSequence(e.value)}
                    placeholder="Select event sequence"
                    />
                </div>

                <div className="field mb-3">
                    <label htmlFor="eventTypes">{strings.eventStore.general.webhooks.dialogs.addWebhook.eventTypes}</label>
                    <MultiSelect
                        id="eventTypes"
                        value={selectedEventTypes}
                        options={allEventTypes.data}
                        onChange={(e) => setSelectedEventTypes(e.value)}
                        optionLabel="id"
                        dataKey="id"
                        placeholder="Select event types"
                        display="chip"
                    />
                </div>

                <div className="field mb-3">
                    <label htmlFor="authType">{strings.eventStore.general.webhooks.dialogs.addWebhook.authType}</label>
                    <Dropdown
                        id="authType"
                        value={authType}
                        options={authTypes}
                        onChange={(e) => setAuthType(e.value)}
                        placeholder="Select authorization type"
                    />
                </div>

                {authType === AuthorizationType.basic && (
                    <>
                        <div className="field mb-3">
                            <label htmlFor="basicUsername">{strings.eventStore.general.webhooks.dialogs.addWebhook.basicUsername}</label>
                            <InputText id="basicUsername" value={basicUsername} onChange={(e) => setBasicUsername(e.target.value)} />
                        </div>
                        <div className="field mb-3">
                            <label htmlFor="basicPassword">{strings.eventStore.general.webhooks.dialogs.addWebhook.basicPassword}</label>
                            <InputText id="basicPassword" type="password" value={basicPassword} onChange={(e) => setBasicPassword(e.target.value)} />
                        </div>
                    </>
                )}

                {authType === AuthorizationType.bearer && (
                    <div className="field mb-3">
                        <label htmlFor="bearerToken">{strings.eventStore.general.webhooks.dialogs.addWebhook.bearerToken}</label>
                        <InputText id="bearerToken" value={bearerToken} onChange={(e) => setBearerToken(e.target.value)} />
                    </div>
                )}

                {authType === AuthorizationType.OAuth && (
                    <>
                        <div className="field mb-3">
                            <label htmlFor="oauthAuthority">{strings.eventStore.general.webhooks.dialogs.addWebhook.oauthAuthority}</label>
                            <InputText id="oauthAuthority" value={oauthAuthority} onChange={(e) => setOauthAuthority(e.target.value)} />
                        </div>
                        <div className="field mb-3">
                            <label htmlFor="oauthClientId">{strings.eventStore.general.webhooks.dialogs.addWebhook.oauthClientId}</label>
                            <InputText id="oauthClientId" value={oauthClientId} onChange={(e) => setOauthClientId(e.target.value)} />
                        </div>
                        <div className="field mb-3">
                            <label htmlFor="oauthClientSecret">{strings.eventStore.general.webhooks.dialogs.addWebhook.oauthClientSecret}</label>
                            <InputText id="oauthClientSecret" type="password" value={oauthClientSecret} onChange={(e) => setOauthClientSecret(e.target.value)} />
                        </div>
                    </>
                )}

                {validationErrors.length > 0 && (
                    <div className="field mb-3">
                        {validationErrors.map((error, index) => (
                            <Message key={index} severity="error" text={error} className="mb-2" />
                        ))}
                    </div>
                )}

                <div className="field flex align-items-center mb-3">
                    <label htmlFor="isActive" className="flex-1">{strings.eventStore.general.webhooks.dialogs.addWebhook.isActive}</label>
                    <InputSwitch inputId="isActive" checked={isActive} onChange={(e) => setIsActive(e.value)} />
                </div>

                <div className="field flex align-items-center">
                    <label htmlFor="isReplayable" className="flex-1">{strings.eventStore.general.webhooks.dialogs.addWebhook.isReplayable}</label>
                    <InputSwitch inputId="isReplayable" checked={isReplayable} onChange={(e) => setIsReplayable(e.value)} />
                </div>
            </div>
        </Dialog>
    );
};
