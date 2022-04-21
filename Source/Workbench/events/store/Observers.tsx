// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { Microservices } from 'API/configuration/Microservices';
import { Microservice } from 'API/configuration/Microservice';
import { AllObservers } from 'API/events/store/observers/AllObservers';
import { Tenants } from 'API/configuration/Tenants';
import { Tenant } from 'API/configuration/Tenant';
import { AllObserversArguments } from 'API/events/store/observers/AllObservers';
import {
    Dropdown,
    IColumn,
    IDropdownOption,
    IDropdownStyles,
    Stack
} from '@fluentui/react';
import { ScrollableDetailsList } from '@aksio/cratis-fluentui';

const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

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
        key: 'offset',
        name: 'Offset',
        fieldName: 'offset',
        minWidth: 200,
    },
    {
        key: 'running-state',
        name: 'State',
        fieldName: 'runningState',
        minWidth: 200,
    },

];

export const Observers = () => {
    const [microservices] = Microservices.use();
    const [tenants] = Tenants.use();

    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();
    const [selectedTenant, setSelectedTenant] = useState<Tenant>();

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

    useEffect(() => {
        if (selectedMicroservice) {
            // refreshEventTypes(getAllEventTypesArguments());
        }
    }, [selectedMicroservice]);

    return (
        <Stack>
            <Stack horizontal>

                <Dropdown
                    styles={commandBarDropdownStyles}
                    options={microserviceOptions}
                    selectedKey={selectedMicroservice?.id}
                    onChange={(e, option) => {
                        setSelectedMicroservice(microservices.data.find(_ => _.id == option!.key));
                    }} />

                <Dropdown
                    styles={commandBarDropdownStyles}
                    options={tenantOptions}
                    selectedKey={selectedTenant?.id}
                    onChange={(e, option) => {
                        setSelectedTenant(tenants.data.find(_ => _.id == option!.key));
                    }} />
            </Stack>

            <ScrollableDetailsList
                columns={columns}
                items={observers.data}
            />
        </Stack>
    );
};
