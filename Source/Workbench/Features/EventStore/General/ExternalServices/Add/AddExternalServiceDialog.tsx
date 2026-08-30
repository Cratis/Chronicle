// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddExternalService } from 'Features/ExternalServices';
import { ExternalServiceEndpointType } from 'Features/Contracts/ExternalServices';
import { AuthorizationType } from 'Features/Contracts/Security';
import { useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, DropdownField, NumberField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export const AddExternalServiceDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext<object>();

    const [endpointType, setEndpointType] = useState(ExternalServiceEndpointType.http);
    const [authType, setAuthType] = useState(AuthorizationType.none);
    const [urlValid, setUrlValid] = useState(false);
    const [hostValid, setHostValid] = useState(false);
    const [databaseValid, setDatabaseValid] = useState(false);

    const endpointTypes = [
        { label: strings.eventStore.general.externalServices.endpointTypes.http, value: ExternalServiceEndpointType.http },
        { label: strings.eventStore.general.externalServices.endpointTypes.msSql, value: ExternalServiceEndpointType.msSql },
        { label: strings.eventStore.general.externalServices.endpointTypes.postgreSql, value: ExternalServiceEndpointType.postgreSql }
    ];

    const authTypes = [
        { label: strings.eventStore.general.externalServices.authTypes.none, value: AuthorizationType.none },
        { label: strings.eventStore.general.externalServices.authTypes.basic, value: AuthorizationType.basic },
        { label: strings.eventStore.general.externalServices.authTypes.bearer, value: AuthorizationType.bearer },
        { label: strings.eventStore.general.externalServices.authTypes.oauth, value: AuthorizationType.OAuth }
    ];

    const isUrlValidFormat = (urlString: string): boolean => {
        try {
            new URL(urlString);
            return true;
        } catch {
            return false;
        }
    };

    const isHttp = endpointType === ExternalServiceEndpointType.http;
    const isValid = isHttp ? urlValid : (hostValid && databaseValid);

    return (
        <CommandDialog
            command={AddExternalService}
            initialValues={{
                eventStore: params.eventStore!,
                endpointType: ExternalServiceEndpointType.http,
                authorizationType: AuthorizationType.none,
                port: 0,
                headers: {} as Record<string, string>,
                options: {} as Record<string, string>
            }}
            isValid={isValid}
            title={strings.eventStore.general.externalServices.dialogs.addExternalService.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="600px"
            onFieldChange={(command, fieldName) => {
                if (fieldName === 'endpointType') {
                    setEndpointType(command.endpointType!);
                }
                if (fieldName === 'authorizationType') {
                    setAuthType(command.authorizationType!);
                }
                if (fieldName === 'url') {
                    setUrlValid(isUrlValidFormat(command.url ?? ''));
                }
                if (fieldName === 'host') {
                    setHostValid((command.host ?? '').trim().length > 0);
                }
                if (fieldName === 'database') {
                    setDatabaseValid((command.database ?? '').trim().length > 0);
                }
            }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <div className="p-fluid">
                <InputTextField<AddExternalService>
                    value={c => c.name}
                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.name}
                />
                <DropdownField<AddExternalService>
                    value={c => c.endpointType}
                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.endpointType}
                    options={endpointTypes}
                    optionValue="value"
                    optionLabel="label"
                    placeholder="Select endpoint type"
                />
                {isHttp && (
                    <>
                        <InputTextField<AddExternalService>
                            value={c => c.url}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.url}
                            type="url"
                        />
                        <DropdownField<AddExternalService>
                            value={c => c.authorizationType}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.authType}
                            options={authTypes}
                            optionValue="value"
                            optionLabel="label"
                            placeholder="Select authorization type"
                            required={false}
                        />
                        {authType === AuthorizationType.basic && (
                            <>
                                <InputTextField<AddExternalService>
                                    value={c => c.basicUsername}
                                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.basicUsername}
                                    required={false}
                                />
                                <InputTextField<AddExternalService>
                                    value={c => c.basicPassword}
                                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.basicPassword}
                                    type="password"
                                    required={false}
                                />
                            </>
                        )}
                        {authType === AuthorizationType.bearer && (
                            <InputTextField<AddExternalService>
                                value={c => c.bearerToken}
                                title={strings.eventStore.general.externalServices.dialogs.addExternalService.bearerToken}
                                required={false}
                            />
                        )}
                        {authType === AuthorizationType.OAuth && (
                            <>
                                <InputTextField<AddExternalService>
                                    value={c => c.OAuthAuthority}
                                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.oauthAuthority}
                                    required={false}
                                />
                                <InputTextField<AddExternalService>
                                    value={c => c.OAuthClientId}
                                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.oauthClientId}
                                    required={false}
                                />
                                <InputTextField<AddExternalService>
                                    value={c => c.OAuthClientSecret}
                                    title={strings.eventStore.general.externalServices.dialogs.addExternalService.oauthClientSecret}
                                    type="password"
                                    required={false}
                                />
                            </>
                        )}
                    </>
                )}
                {!isHttp && (
                    <>
                        <InputTextField<AddExternalService>
                            value={c => c.host}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.host}
                        />
                        <NumberField<AddExternalService>
                            value={c => c.port}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.port}
                            required={false}
                        />
                        <InputTextField<AddExternalService>
                            value={c => c.database}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.database}
                        />
                        <InputTextField<AddExternalService>
                            value={c => c.username}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.username}
                            required={false}
                        />
                        <InputTextField<AddExternalService>
                            value={c => c.password}
                            title={strings.eventStore.general.externalServices.dialogs.addExternalService.password}
                            type="password"
                            required={false}
                        />
                    </>
                )}
            </div>
        </CommandDialog>
    );
};
