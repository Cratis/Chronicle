// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import strings from 'Strings';
import { AllGrants, FederationGrantInformation, RevokeFederationLink, SetFederationGrant } from 'Features/Federation';
import { Button } from '@cratis/components/Common';
import { Column, DataTableCore } from '@cratis/components/DataTables';
import { DialogButtons, DialogResult, useConfirmationDialog, useDialog } from '@cratis/arc.react/dialogs';
import { GrantDialog, GrantDialogRequest } from './GrantDialog';
import { formatBytes } from '../federationValidation';
import css from '../Federation.module.css';

const text = () => strings.federation.grants;

const formatDate = (date?: Date | string) => date ? new Date(date).toLocaleString() : '';

export const ProducerGrants = () => {
    const [grants, refresh] = AllGrants.use();
    const [selected, setSelected] = useState<FederationGrantInformation | null>(null);
    const [showConfirmation] = useConfirmationDialog();
    const [GrantDialogWrapper, showGrantDialog] = useDialog<object, GrantDialogRequest>(GrantDialog);
    const [revokeLink] = RevokeFederationLink.use();
    const [setGrant] = SetFederationGrant.use();

    const edit = async (grant?: FederationGrantInformation) => {
        const [result] = await showGrantDialog({ grant });
        if (result === DialogResult.Ok) {
            refresh();
        }
    };

    const revoke = async () => {
        if (!selected) {
            return;
        }
        const result = await showConfirmation(
            text().revoke.title,
            text().revoke.message.replace('{id}', selected.id),
            DialogButtons.YesNo);
        if (result === DialogResult.Yes) {
            revokeLink.id = selected.id;
            await revokeLink.execute();
            setSelected(null);
            refresh();
        }
    };

    const reinstate = async () => {
        if (!selected) {
            return;
        }
        setGrant.id = selected.id;
        setGrant.clientPin = selected.clientPin;
        setGrant.eventStore = selected.eventStore;
        setGrant.namespaces = selected.namespaces;
        setGrant.allPublic = selected.allPublic;
        setGrant.eventTypes = selected.eventTypes;
        await setGrant.execute();
        setSelected(null);
        refresh();
    };

    return (
        <section aria-labelledby="federation-grants-heading">
            <h2 id="federation-grants-heading">{text().title}</h2>
            <p>{text().direction}</p>
            <div className={css.toolbar} role="toolbar" aria-label={text().title}>
                <Button label={text().actions.add} icon="pi pi-plus" onClick={() => edit()} />
                <Button label={text().actions.edit} icon="pi pi-pencil" variant="outline" disabled={!selected} onClick={() => edit(selected ?? undefined)} />
                <Button label={text().actions.revoke} icon="pi pi-ban" variant="outline" tone="critical" disabled={!selected || selected.revoked} onClick={revoke} />
                <Button label={text().actions.reinstate} icon="pi pi-replay" variant="outline" disabled={!selected || !selected.revoked} onClick={reinstate} />
                <Button label={strings.federation.refresh} icon="pi pi-refresh" variant="ghost" onClick={() => refresh()} />
            </div>
            <DataTableCore<FederationGrantInformation>
                data={grants.data ?? []}
                dataKey="id"
                emptyMessage={text().empty}
                selectionMode="single"
                selection={selected}
                onSelectionChange={event => setSelected(event.value as FederationGrantInformation)}>
                <Column field="id" header={text().columns.id} sortable />
                <Column field="clientPin" header={text().columns.clientPin} />
                <Column field="eventStore" header={text().columns.eventStore} sortable />
                <Column header={text().columns.namespaces} body={(grant: FederationGrantInformation) => (grant.namespaces ?? []).join(', ')} />
                <Column header={text().columns.scope} body={(grant: FederationGrantInformation) => grant.allPublic ? text().allPublic : (grant.eventTypes ?? []).join(', ')} />
                <Column field="revoked" header={text().columns.state} sortable body={(grant: FederationGrantInformation) => grant.revoked ? text().revokedState : text().activeState} />
                <Column field="origin" header={text().columns.origin} body={(grant: FederationGrantInformation) => <span className={css.badge}>{grant.origin}</span>} />
                <Column field="lastPull" header={text().columns.lastPull} body={(grant: FederationGrantInformation) => formatDate(grant.lastPull)} />
                <Column field="eventsDelivered" header={text().columns.eventsDelivered} sortable />
                <Column field="bytesDelivered" header={text().columns.bytesDelivered} sortable body={(grant: FederationGrantInformation) => formatBytes(grant.bytesDelivered)} />
                <Column field="throttled" header={text().columns.throttled} sortable />
            </DataTableCore>
            <GrantDialogWrapper />
        </section>
    );
};
