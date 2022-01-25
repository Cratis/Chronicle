// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ModalButtons, ScrollableDetailsList, useModal, ModalResult, IModalProps } from '@aksio/cratis-fluentui';
import { CommandBar, IColumn, ICommandBarItemProps, IconButton, Panel, SearchBox, Selection, SelectionMode, Stack, TextField } from '@fluentui/react';
import { AllPeople } from 'API/compliance/gdpr/people/AllPeople';
import { useMemo, useState } from 'react';
import { Person } from 'API/compliance/gdpr/people/Person';
import { useBoolean } from '@fluentui/react-hooks';
import { SearchForPeople } from '../API/compliance/gdpr/people/SearchForPeople';
import { CreateAndRegisterKeyFor } from '../API/compliance/gdpr/pii/CreateAndRegisterKeyFor';
import { AddKeyDialog } from './AddKeyDialog';
import { DeletePIIForPerson } from '../API/compliance/gdpr/pii/DeletePIIForPerson';

const columns: IColumn[] = [
    {
        key: 'socialSecurityNumber',
        name: 'Social Security Number',
        fieldName: 'socialSecurityNumber',
        minWidth: 150,
        maxWidth: 150
    },
    {
        key: 'firstName',
        name: 'First Name',
        fieldName: 'firstName',
        minWidth: 200
    },
    {
        key: 'lastName',
        name: 'Last Name',
        fieldName: 'lastName',
        minWidth: 200
    }
];



export const People = () => {
    const [searching, setSearching] = useState(false);
    const [people] = AllPeople.use();
    const [isDetailsPanelOpen, { setTrue: openPanel, setFalse: dismissPanel }] = useBoolean(false);
    const [selectedPerson, setSelectedPerson] = useState<Person>();
    const [showDeletePIIWarning] = useModal(
        `Delete PII? `,
        ModalButtons.OkCancel,
        `Are you sure you want to delete PII for '${selectedPerson?.firstName} ${selectedPerson?.lastName} (${selectedPerson?.socialSecurityNumber})'?`,
        async (result) => {
            if (result == ModalResult.Success && selectedPerson) {
                const command = new DeletePIIForPerson();
                command.personId = selectedPerson.id;
                await command.execute();
            }
        });
    const [showAddKeyDialog] = useModal(
        'Create Encryption Key',
        ModalButtons.OkCancel,
        AddKeyDialog,
        async (result, output) => {
            if (result == ModalResult.Success && output) {
                const command = new CreateAndRegisterKeyFor();
                command.identifier = output?.identifier;
                await command.execute();
            }
        }
    );

    //const [searchPeople, searchForPeople] = SearchForPeople.use({ query: '' });
    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'crateKey',
            name: 'Create Key',
            iconProps: { iconName: 'AzureKeyVault' },
            onClick: showAddKeyDialog
        }
        // {
        //     key: 'search',
        //     onRender: (props, defaultRenderer) => {
        //         return (
        //             <div style={{ position: 'relative', top: '6px', width: '400px' }}>
        //                 <SearchBox
        //                     placeholder="Person"
        //                     onClear={() => {
        //                         setSearching(false);
        //                     }}
        //                     onChange={(ev, newValue) => {
        //                         setSearching(true);
        //                         searchForPeople({ query: newValue! });
        //                     }} />
        //             </div>
        //         );
        //     }
        // }
    ];
    //const data = searching ? searchPeople.data : people.data;
    const data = people.data;

    if (selectedPerson) {
        commandBarItems.push(
            {
                key: 'delete',
                name: 'Delete PII',
                iconProps: { iconName: 'Delete' },
                onClick: showDeletePIIWarning
            },
            {
                key: 'details',
                name: 'Show PII details',
                iconProps: { iconName: 'ComplianceAudit' },
                onClick: openPanel
            });
    }

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,

            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelectedPerson(selected[0] as Person);
                } else {
                    setSelectedPerson(undefined);
                }
            },
            items: people.data as any
        }), [people.data]);

    return (
        <>
            <Stack style={{ height: '100%' }}>
                <Stack.Item>
                    <CommandBar items={commandBarItems} />
                </Stack.Item>
                <Stack.Item>
                    <ScrollableDetailsList
                        columns={columns}
                        items={data}
                        selection={selection} />
                </Stack.Item>
            </Stack>

            {isDetailsPanelOpen &&
                <Panel
                    isLightDismiss
                    isOpen={isDetailsPanelOpen}
                    onDismiss={() => {
                        selection.toggleAllSelected();
                        dismissPanel();
                    }}
                    headerText={`${selectedPerson?.firstName} - ${selectedPerson?.lastName}`}>
                    <TextField label="Identifier" disabled defaultValue={selectedPerson?.id as any} />
                    <TextField label="Social Security Number" disabled defaultValue={selectedPerson?.socialSecurityNumber as any} />
                    {
                        (selectedPerson && selectedPerson.personalInformation) && selectedPerson.personalInformation.map((_, index) => <TextField key={index} label={_.type} disabled defaultValue={_.value as any} />)
                    }
                </Panel>
            }
        </>
    );
};
