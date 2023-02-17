// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandBar,
    ICommandBarItemProps,
    Dropdown,
    IDropdownOption,
    IDropdownStyles,
    Stack,
    Selection,
    SelectionMode
} from '@fluentui/react';
import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';
import { useEffect, useState, useMemo } from 'react';
import { Microservice } from 'API/configuration/microservices/Microservice';
import { ScrollableDetailsList } from '@aksio/cratis-fluentui';
import { IColumn, Panel, TextField } from '@fluentui/react';
import { AllFailedPartitions } from 'API/events/store/failed-partitions/AllFailedPartitions';
import { AllTenants } from '../../API/configuration/tenants/AllTenants';
import { TenantInfo } from '../../API/configuration/tenants/TenantInfo';
import { RecoverFailedPartitionState } from '../../API/events/store/failed-partitions/RecoverFailedPartitionState';
import { useBoolean } from '@fluentui/react-hooks';


const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

const columns: IColumn[] = [
    {
        key: 'observerId',
        name: 'Observer Id',
        fieldName: 'observerId',
        minWidth: 250,
        maxWidth: 250,
    },
    {
        key: 'observerName',
        name: 'Observer Name',
        fieldName: 'observerName',
        minWidth: 250,
        maxWidth: 250,
    },
    {
        key: 'occurred',
        name: 'Occurred',
        fieldName: 'initialPartitionFailedOn',
        minWidth: 250,
        maxWidth: 250,
        onRender: (item: RecoverFailedPartitionState) => {
            return (
                <>{item.initialPartitionFailedOn ? new Date(item.initialPartitionFailedOn).toLocaleString() : ''}</>
            );
        }

    }
];

export const FailedPartitions = () => {
    const [microservices] = AllMicroservices.use();
    const [tenants] = AllTenants.use();
    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [selectedItem, setSelectedItem] = useState<RecoverFailedPartitionState>();

    const [failedPartitions] = AllFailedPartitions.use({
        microserviceId: selectedMicroservice?.id ?? undefined!,
        tenantId: selectedTenant?.id ?? undefined!
    });

    const microserviceOptions = microservices.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    const tenantOptions = tenants.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    useEffect(() => {
        if (microservices.data.length > 0) {
            setSelectedMicroservice(microservices.data[0]);
        }
    }, [microservices.data]);

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);

    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'microservice',
            text: 'Microservice',
            onRender: () => {
                return (
                    <Dropdown
                        styles={commandBarDropdownStyles}
                        options={microserviceOptions}
                        selectedKey={selectedMicroservice?.id}
                        onChange={(e, option) => {
                            setSelectedMicroservice(microservices.data.find(_ => _.id == option!.key));
                        }} />
                );
            }
        },
        {
            key: 'tenant',
            text: 'Tenant',
            onRender: () => {
                return (
                    <Dropdown
                        styles={commandBarDropdownStyles}
                        options={tenantOptions}
                        selectedKey={selectedTenant?.id}
                        onChange={(e, option) => {
                            setSelectedTenant(tenants.data.find(_ => _.id == option!.key));
                        }} />
                );
            }
        }
    ];

    const closePanel = () => {
        setSelectedItem(undefined);
        dismissPanel();
    };

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelectedItem(selected[0] as RecoverFailedPartitionState);
                    openPanel();
                }
            },
            items: failedPartitions.data as any[]
        }), [failedPartitions.data]);


    return (
        <>
            <Stack style={{ height: '100%' }}>
                <CommandBar items={commandBarItems} />
                <ScrollableDetailsList
                    columns={columns}
                    items={failedPartitions.data}
                    selection={selection}

                />
            </Stack>
            <Panel
                isLightDismiss
                isOpen={isDetailsPanelOpen}
                onDismiss={closePanel}
                headerText={selectedItem?.id}>
                <TextField label="Occurred" disabled defaultValue={selectedItem?.initialPartitionFailedOn.toLocaleDateString() ?? new Date().toLocaleString()} />
                {
                    (selectedItem) && Object.keys(selectedItem).map(_ => <TextField key={_} label={_} disabled defaultValue={selectedItem![_]} />)
                }
            </Panel>
        </>

    );
};
