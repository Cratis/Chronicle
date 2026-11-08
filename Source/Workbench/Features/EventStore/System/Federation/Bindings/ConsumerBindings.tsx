// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import strings from 'Strings';
import { AllBindings, FederationBinding, UnbindFederationPeer } from 'Features/Federation';
import { Button } from '@cratis/components/Common';
import { Column, DataTableCore } from '@cratis/components/DataTables';
import { DialogButtons, DialogResult, useConfirmationDialog, useDialog } from '@cratis/arc.react/dialogs';
import { BindingDialog, BindingDialogRequest } from './BindingDialog';
import css from '../Federation.module.css';

const text = () => strings.federation.bindings;

const formatDate = (date?: Date | string) => date ? new Date(date).toLocaleString() : '';

export const ConsumerBindings = () => {
    const [bindings, refresh] = AllBindings.use();
    const [selected, setSelected] = useState<FederationBinding | null>(null);
    const [showConfirmation] = useConfirmationDialog();
    const [BindingDialogWrapper, showBindingDialog] = useDialog<object, BindingDialogRequest>(BindingDialog);
    const [unbind] = UnbindFederationPeer.use();

    const edit = async (binding?: FederationBinding) => {
        const [result] = await showBindingDialog({ binding });
        if (result === DialogResult.Ok) {
            refresh();
        }
    };

    const remove = async () => {
        if (!selected) {
            return;
        }
        const result = await showConfirmation(
            text().remove.title,
            text().remove.message.replace('{id}', selected.id),
            DialogButtons.YesNo);
        if (result === DialogResult.Yes) {
            unbind.id = selected.id;
            await unbind.execute();
            setSelected(null);
            refresh();
        }
    };

    return (
        <section aria-labelledby="federation-bindings-heading">
            <h2 id="federation-bindings-heading">{text().title}</h2>
            <p>{text().direction}</p>
            <div className={css.toolbar} role="toolbar" aria-label={text().title}>
                <Button label={text().actions.add} icon="pi pi-plus" onClick={() => edit()} />
                <Button label={text().actions.edit} icon="pi pi-pencil" variant="outline" disabled={!selected} onClick={() => edit(selected ?? undefined)} />
                <Button label={text().actions.remove} icon="pi pi-trash" variant="outline" tone="critical" disabled={!selected} onClick={remove} />
                <Button label={strings.federation.refresh} icon="pi pi-refresh" variant="ghost" onClick={() => refresh()} />
            </div>
            <DataTableCore<FederationBinding>
                data={bindings.data ?? []}
                dataKey="id"
                emptyMessage={text().empty}
                selectionMode="single"
                selection={selected}
                onSelectionChange={event => setSelected(event.value as FederationBinding)}>
                <Column field="id" header={text().columns.id} sortable />
                <Column field="logicalSource" header={text().columns.logicalSource} sortable />
                <Column field="endpoint" header={text().columns.endpoint} />
                <Column header={text().columns.remote} body={(binding: FederationBinding) => `${binding.remoteEventStore} / ${binding.remoteNamespace}`} />
                <Column header={text().columns.local} body={(binding: FederationBinding) => `${binding.targetEventStore} / ${binding.targetNamespace}`} />
                <Column field="origin" header={text().columns.origin} body={(binding: FederationBinding) => <span className={css.badge}>{binding.origin}</span>} />
                <Column field="state" header={text().columns.state} sortable />
                <Column field="lag" header={text().columns.lag} sortable body={(binding: FederationBinding) => `${binding.lag} (${binding.lastSourceSequenceNumber} / ${binding.remoteTailSequenceNumber})`} />
                <Column field="lastDelivery" header={text().columns.lastDelivery} body={(binding: FederationBinding) => formatDate(binding.lastDelivery)} />
                <Column field="lastError" header={text().columns.lastError} />
            </DataTableCore>
            <BindingDialogWrapper />
        </section>
    );
};
