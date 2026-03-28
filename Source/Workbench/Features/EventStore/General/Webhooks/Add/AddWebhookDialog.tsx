// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddWebHook } from 'Api/Webhooks';
import { AllEventSequences } from 'Api/EventSequences';
import { AllEventTypes } from 'Api/EventTypes';
import { EventType } from 'Api/Events';
import { AuthorizationType } from 'Api/Security';
import { MultiSelect } from 'primereact/multiselect';
import { Message } from 'primereact/message';
import { Button } from 'primereact/button';
import { useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, DropdownField, CheckboxField } from '@cratis/components/CommandForm';
import { useCommandInstance } from '@cratis/arc.react/commands';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

interface TestButtonProps {
    isValid: boolean;
    onTestResult: (success: boolean, errors: string[]) => void;
}

const WebhookTestButton = ({ isValid, onTestResult }: TestButtonProps) => {
    const command = useCommandInstance<AddWebHook>();

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
            errors.push('An unexpected error occurred while testing the webhook.');
        }
        return errors;
    };

    const handleTest = async () => {
        const validationResult = await command.validate();
        if (validationResult.isSuccess) {
            onTestResult(true, []);
        } else {
            onTestResult(false, extractErrors(validationResult));
        }
    };

    return (
        <Button
            label={strings.eventStore.general.webhooks.dialogs.addWebhook.test}
            icon="pi pi-check-circle"
            severity="secondary"
            disabled={!isValid}
            onClick={handleTest}
        />
    );
};

export const AddWebhookDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext<object>();

    const [allEventSequences] = AllEventSequences.use({ eventStore: params.eventStore! });
    const [allEventTypes] = AllEventTypes.use({ eventStore: params.eventStore! });

    const [selectedEventTypes, setSelectedEventTypes] = useState<EventType[]>([]);
    const [authType, setAuthType] = useState(AuthorizationType.none);
    const [urlValid, setUrlValid] = useState(false);
    const [validationErrors, setValidationErrors] = useState<string[]>([]);
    const [testSuccess, setTestSuccess] = useState(false);

    const authTypes = [
        { label: strings.eventStore.general.webhooks.authTypes.none, value: AuthorizationType.none },
        { label: strings.eventStore.general.webhooks.authTypes.basic, value: AuthorizationType.basic },
        { label: strings.eventStore.general.webhooks.authTypes.bearer, value: AuthorizationType.bearer },
        { label: strings.eventStore.general.webhooks.authTypes.oauth, value: AuthorizationType.OAuth }
    ];

    const eventSequenceOptions = allEventSequences.data.map(seq => ({ label: seq, value: seq }));

    const isUrlValidFormat = (urlString: string): boolean => {
        try {
            new URL(urlString);
            return true;
        } catch {
            return false;
        }
    };

    const isValid = urlValid && selectedEventTypes.length > 0;

    return (
        <CommandDialog
            command={AddWebHook}
            initialValues={{
                eventStore: params.eventStore!,
                eventSequenceId: 'event-log',
                authorizationType: AuthorizationType.none,
                isActive: true,
                isReplayable: true,
                headers: {} as Record<string, unknown>
            }}
            currentValues={{ eventTypes: selectedEventTypes }}
            isValid={isValid}
            title={strings.eventStore.general.webhooks.dialogs.addWebhook.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="600px"
            onFieldChange={(command, fieldName) => {
                if (fieldName === 'url') {
                    setUrlValid(isUrlValidFormat(command.url ?? ''));
                }
                if (fieldName === 'authorizationType') {
                    setAuthType(command.authorizationType!);
                }
            }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <div className="p-fluid">
                <InputTextField<AddWebHook>
                    value={c => c.name}
                    title={strings.eventStore.general.webhooks.dialogs.addWebhook.name}
                />
                <InputTextField<AddWebHook>
                    value={c => c.url}
                    title={strings.eventStore.general.webhooks.dialogs.addWebhook.url}
                    type="url"
                />
                <DropdownField<AddWebHook>
                    value={c => c.eventSequenceId}
                    title={strings.eventStore.general.webhooks.dialogs.addWebhook.eventSequence}
                    options={eventSequenceOptions}
                    optionValue="value"
                    optionLabel="label"
                    placeholder="Select event sequence"
                />
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
                <DropdownField<AddWebHook>
                    value={c => c.authorizationType}
                    title={strings.eventStore.general.webhooks.dialogs.addWebhook.authType}
                    options={authTypes}
                    optionValue="value"
                    optionLabel="label"
                    placeholder="Select authorization type"
                    required={false}
                />
                {authType === AuthorizationType.basic && (
                    <>
                        <InputTextField<AddWebHook>
                            value={c => c.basicUsername}
                            title={strings.eventStore.general.webhooks.dialogs.addWebhook.basicUsername}
                            required={false}
                        />
                        <InputTextField<AddWebHook>
                            value={c => c.basicPassword}
                            title={strings.eventStore.general.webhooks.dialogs.addWebhook.basicPassword}
                            type="password"
                            required={false}
                        />
                    </>
                )}
                {authType === AuthorizationType.bearer && (
                    <InputTextField<AddWebHook>
                        value={c => c.bearerToken}
                        title={strings.eventStore.general.webhooks.dialogs.addWebhook.bearerToken}
                        required={false}
                    />
                )}
                {authType === AuthorizationType.OAuth && (
                    <>
                        <InputTextField<AddWebHook>
                            value={c => c.OAuthAuthority}
                            title={strings.eventStore.general.webhooks.dialogs.addWebhook.oauthAuthority}
                            required={false}
                        />
                        <InputTextField<AddWebHook>
                            value={c => c.OAuthClientId}
                            title={strings.eventStore.general.webhooks.dialogs.addWebhook.oauthClientId}
                            required={false}
                        />
                        <InputTextField<AddWebHook>
                            value={c => c.OAuthClientSecret}
                            title={strings.eventStore.general.webhooks.dialogs.addWebhook.oauthClientSecret}
                            type="password"
                            required={false}
                        />
                    </>
                )}
                {validationErrors.length > 0 && (
                    <div className="field mb-3">
                        {validationErrors.map((error, index) => (
                            <Message key={index} severity="error" text={error} className="mb-2" />
                        ))}
                    </div>
                )}
                {testSuccess && (
                    <div className="field mb-3">
                        <Message severity="success" text={strings.eventStore.general.webhooks.dialogs.addWebhook.testSuccess} className="mb-2" />
                    </div>
                )}
                <CheckboxField<AddWebHook>
                    value={c => c.isActive}
                    title={strings.eventStore.general.webhooks.dialogs.addWebhook.isActive}
                />
                <CheckboxField<AddWebHook>
                    value={c => c.isReplayable}
                    title={strings.eventStore.general.webhooks.dialogs.addWebhook.isReplayable}
                />
                <div className="field">
                    <WebhookTestButton
                        isValid={isValid}
                        onTestResult={(success, errors) => {
                            setTestSuccess(success);
                            setValidationErrors(errors);
                        }} />
                </div>
            </div>
        </CommandDialog>
    );
};
