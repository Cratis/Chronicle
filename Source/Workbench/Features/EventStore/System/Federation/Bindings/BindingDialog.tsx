// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import strings from 'Strings';
import { BindFederationPeer, FederationBinding } from 'Features/Federation';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { isNotBlank, isValidHttpsEndpoint, isValidPin } from '../federationValidation';

/**
 * The request for the {@link BindingDialog}. A binding means editing; none means adding.
 */
export interface BindingDialogRequest {
    binding?: FederationBinding;
}

const text = () => strings.federation.bindings.dialog;

export const BindingDialog = () => {
    const { request, closeDialog } = useDialogContext<BindingDialogRequest>();
    const binding = request?.binding;
    const [initialValues] = useState<Partial<BindFederationPeer>>({
        id: binding?.id,
        logicalSource: binding?.logicalSource,
        endpoint: binding?.endpoint,
        serverPin: binding?.serverPin,
        clientCertificate: binding?.clientCertificate,
        remoteEventStore: binding?.remoteEventStore,
        remoteNamespace: binding?.remoteNamespace,
        targetEventStore: binding?.targetEventStore,
        targetNamespace: binding?.targetNamespace
    });
    const [values, setValues] = useState<Partial<BindFederationPeer>>(initialValues);

    const pinInvalid = isNotBlank(values.serverPin) && !isValidPin(values.serverPin);
    const endpointInvalid = isNotBlank(values.endpoint) && !isValidHttpsEndpoint(values.endpoint);

    const isValid =
        isNotBlank(values.id) &&
        isNotBlank(values.logicalSource) &&
        isValidHttpsEndpoint(values.endpoint) &&
        isValidPin(values.serverPin) &&
        isNotBlank(values.clientCertificate) &&
        isNotBlank(values.remoteEventStore) &&
        isNotBlank(values.remoteNamespace) &&
        isNotBlank(values.targetEventStore) &&
        isNotBlank(values.targetNamespace);

    return (
        <CommandDialog
            command={BindFederationPeer}
            initialValues={initialValues}
            isValid={isValid}
            title={binding ? text().editTitle : text().addTitle}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="640px"
            onFieldChange={(command) => setValues({
                id: command.id,
                logicalSource: command.logicalSource,
                endpoint: command.endpoint,
                serverPin: command.serverPin,
                clientCertificate: command.clientCertificate,
                remoteEventStore: command.remoteEventStore,
                remoteNamespace: command.remoteNamespace,
                targetEventStore: command.targetEventStore,
                targetNamespace: command.targetNamespace
            })}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <p>{text().direction}</p>
            <InputTextField<BindFederationPeer> value={c => c.id} title={text().id} />
            <InputTextField<BindFederationPeer> value={c => c.logicalSource} title={text().logicalSource} />
            <InputTextField<BindFederationPeer> value={c => c.endpoint} title={text().endpoint} type="url" placeholder="https://peer.example.com:35000" />
            {endpointInvalid && <p role="alert">{text().endpointInvalid}</p>}
            <InputTextField<BindFederationPeer> value={c => c.serverPin} title={text().serverPin} placeholder="sha256:…" />
            {pinInvalid && <p role="alert">{text().pinInvalid}</p>}
            <InputTextField<BindFederationPeer> value={c => c.clientCertificate} title={text().clientCertificate} />
            <p>{text().clientCertificateHelp}</p>
            <InputTextField<BindFederationPeer> value={c => c.remoteEventStore} title={text().remoteEventStore} />
            <InputTextField<BindFederationPeer> value={c => c.remoteNamespace} title={text().remoteNamespace} />
            <InputTextField<BindFederationPeer> value={c => c.targetEventStore} title={text().targetEventStore} />
            <InputTextField<BindFederationPeer> value={c => c.targetNamespace} title={text().targetNamespace} />
        </CommandDialog>
    );
};
