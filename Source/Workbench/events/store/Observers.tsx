// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useMemo } from 'react';
import { Microservices } from 'API/configuration/Microservices';
import { Microservice } from 'API/configuration/Microservice';
import { AllObservers } from 'API/events/store/observers/AllObservers';
import { Tenants } from 'API/configuration/Tenants';
import { Tenant } from 'API/configuration/Tenant';
import { AllObserversArguments } from 'API/events/store/observers/AllObservers';
import {
    CommandBar,
    Dropdown,
    IColumn,
    ICommandBarItemProps,
    IDropdownOption,
    IDropdownStyles,
    Selection,
    SelectionMode,
    Stack
} from '@fluentui/react';
import { ScrollableDetailsList } from '@aksio/cratis-fluentui';
import { ObserverState } from 'API/events/store/observers/ObserverState';
import { Rewind } from 'API/events/store/observers/Rewind';

const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

const observerRunningStates: { [key: number]: string; } = {
    0: 'Unknown',
    1: 'Subscribing',
    2: 'Rewinding',
    3: 'Replaying',
    4: 'Catching up',
    5: 'Active',
    6: 'Paused',
    7: 'Stopped',
    8: 'Suspended',
    9: 'Failed',
    10: 'Tail of replay'
};

const observerTypes: { [key: number]: string; } = {
    0: 'Unknown',
    1: 'Client',
    2: 'Projection'
};

const columns: IColumn[] = [
    {
        key: 'id',
        name: 'Id',
        fieldName: 'observerId',
        minWidth: 250,
        maxWidth: 250,
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 300,
    },
    {
        key: 'type',
        name: 'Type',
        minWidth: 200,
        onRender: (item: ObserverState) => {
            return (
                <>{observerTypes[item.type as number]}</>
            );
        }
    },
    {
        key: 'running-state',
        name: 'State',
        minWidth: 200,
        onRender: (item: ObserverState) => {
            return (
                <>{observerRunningStates[item.runningState as number]}</>
            );
        }
    },
    {
        key: 'offset',
        name: 'Offset',
        fieldName: 'offset',
        minWidth: 200,
    },
];

export const Observers = () => {
    const [microservices] = Microservices.use();
    const [tenants] = Tenants.use();

    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();
    const [selectedTenant, setSelectedTenant] = useState<Tenant>();

    const [selectedObserver, setSelectedObserver] = useState<ObserverState>();

    const getAllObserversArguments = () => {
        return {
            microserviceId: selectedMicroservice?.id || undefined,
            tenantId: selectedTenant?.id || undefined
        } as AllObserversArguments;
    };

    const [observers] = AllObservers.use(getAllObserversArguments());

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

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length == 1) {
                    setSelectedObserver(selected[0] as ObserverState);
                }
            }
        }), [observers.data]);

    const [rewindCommand, setRewindCommandVales] = Rewind.use();

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
        },
        {
            key: 'rewind',
            text: 'Rewind',
            disabled: !selectedObserver,
            onClick: () => {
                setRewindCommandVales({
                    observerId: selectedObserver?.observerId,
                    microserviceId: selectedMicroservice?.id,
                    tenantId: selectedTenant?.id
                });
                rewindCommand.execute();
            },
            iconProps: { iconName: 'Rewind' }
        }
    ];

    return (
        <Stack>
            <CommandBar items={commandBarItems} />

            <ScrollableDetailsList
                columns={columns}
                selection={selection}
                items={observers.data}
            />
        </Stack>
    );
};
