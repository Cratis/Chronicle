// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import strings from 'Strings';
import { FederationGrantInformation, SetFederationGrant } from 'Features/Federation';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { CheckboxField, InputTextField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { formatList, isNotBlank, isValidPin, parseList } from '../federationValidation';

/**
 * The request for the {@link GrantDialog}. A grant means editing; none means adding.
 */
export interface GrantDialogRequest {
    grant?: FederationGrantInformation;
}

const text = () => strings.federation.grants.dialog;

export const GrantDialog = () => {
    const { request, closeDialog } = useDialogContext<GrantDialogRequest>();
    const grant = request?.grant;
    const [initialValues] = useState({
        id: grant?.id,
        clientPin: grant?.clientPin,
        eventStore: grant?.eventStore,
        allPublic: grant?.allPublic ?? true
    });
    const [values, setValues] = useState<Partial<SetFederationGrant>>(initialValues);
    const [namespacesText, setNamespacesText] = useState(formatList(grant?.namespaces));
    const [eventTypesText, setEventTypesText] = useState(formatList(grant?.eventTypes));

    const namespaces = parseList(namespacesText);
    const eventTypes = parseList(eventTypesText);
    const allPublic = values.allPublic === true;
    const pinInvalid = isNotBlank(values.clientPin) && !isValidPin(values.clientPin);

    const isValid =
        isNotBlank(values.id) &&
        isValidPin(values.clientPin) &&
        isNotBlank(values.eventStore) &&
        namespaces.length > 0 &&
        (allPublic || eventTypes.length > 0);

    return (
        <CommandDialog
            command={SetFederationGrant}
            initialValues={initialValues}
            currentValues={{ namespaces, eventTypes: allPublic ? [] : eventTypes }}
            isValid={isValid}
            title={grant ? text().editTitle : text().addTitle}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="640px"
            onFieldChange={(command) => setValues({
                id: command.id,
                clientPin: command.clientPin,
                eventStore: command.eventStore,
                allPublic: command.allPublic
            })}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <p>{text().direction}</p>
            <InputTextField<SetFederationGrant> value={c => c.id} title={text().id} />
            <InputTextField<SetFederationGrant> value={c => c.clientPin} title={text().clientPin} placeholder="sha256:…" />
            {pinInvalid && <p role="alert">{text().pinInvalid}</p>}
            <InputTextField<SetFederationGrant> value={c => c.eventStore} title={text().eventStore} />
            <div className="field">
                <label htmlFor="federation-grant-namespaces">{text().namespaces}</label>
                <input
                    id="federation-grant-namespaces"
                    className="p-inputtext"
                    value={namespacesText}
                    onChange={event => setNamespacesText(event.target.value)}
                    aria-describedby="federation-grant-namespaces-help" />
                <small id="federation-grant-namespaces-help">{text().listHelp}</small>
            </div>
            <CheckboxField<SetFederationGrant> value={c => c.allPublic} title={text().allPublic} />
            {!allPublic && (
                <div className="field">
                    <label htmlFor="federation-grant-event-types">{text().eventTypes}</label>
                    <input
                        id="federation-grant-event-types"
                        className="p-inputtext"
                        value={eventTypesText}
                        onChange={event => setEventTypesText(event.target.value)}
                        aria-describedby="federation-grant-event-types-help" />
                    <small id="federation-grant-event-types-help">{text().listHelp}</small>
                </div>
            )}
        </CommandDialog>
    );
};
