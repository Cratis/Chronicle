// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandBar,
    ICommandBarItemProps,
    Dropdown,
    IDropdownOption,
    IDropdownStyles,
    Stack
} from '@fluentui/react';
import { AllMicroservices } from 'API/configuration/microservices/AllMicroservices';
import { useEffect, useState } from 'react';
import { Microservice } from 'API/configuration/microservices/Microservice';
import { ScrollableDetailsList } from '@aksio/cratis-fluentui';
import { IColumn } from '@fluentui/react';
import { ConnectedClientsForMicroservice } from 'API/clients/ConnectedClientsForMicroservice';

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
    }
];

export const FailedPartitions = () => {
    const [microservices] = AllMicroservices.use();
    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();

    const microserviceOptions = microservices.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    useEffect(() => {

    }, [selectedMicroservice]);

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
        }
    ];

    return (
        <Stack style={{ height: '100%' }}>
            <CommandBar items={commandBarItems} />
            <ScrollableDetailsList
                columns={columns}
                items={connectedClients.data}
            />

        </Stack>
    );
};
