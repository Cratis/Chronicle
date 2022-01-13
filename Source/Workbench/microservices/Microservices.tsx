// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ScrollableDetailsList, useModal, ModalButtons, ModalResult } from '@cratis/fluentui';
import { CommandBar, IColumn, ICommandBarItemProps, Stack } from '@fluentui/react';
import { AllMicroservices } from 'API/compliance/microservices/AllMicroservices';
import { AddMicroserviceDialog, AddMicroserviceDialogResult } from './AddMicroserviceDialog';
import { AddMicroservice } from 'API/compliance/microservices/AddMicroservice';
import { Guid } from '@cratis/fundamentals';

const columns: IColumn[] = [
    {
        key: 'id',
        name: 'Id',
        fieldName: 'id',
        minWidth: 100,
        maxWidth: 100
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    }
];


export const Microservices = () => {

    const [allMicroservices] = AllMicroservices.use();

    const [showAddMicroservicesDialog] = useModal<{}, AddMicroserviceDialogResult>(
        "Add Microservice",
        ModalButtons.OkCancel,
        AddMicroserviceDialog, async (result, output) => {
            if (result == ModalResult.Success && output) {
                const command = new AddMicroservice();
                command.microserviceId = Guid.create().toString();
                command.name = output.name;
                await command.execute();
            }
        });

    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'add',
            name: 'Add',
            iconProps: { iconName: 'Add' },
            onClick: () => showAddMicroservicesDialog()
        }
    ];

    return (
        <>
            <Stack style={{ height: '100%' }}>
                <Stack.Item>
                    <CommandBar items={commandBarItems} />
                </Stack.Item>
                <Stack.Item>
                    <ScrollableDetailsList
                        columns={columns}
                        items={allMicroservices.data} />
                </Stack.Item>
            </Stack>
        </>
    );
};
