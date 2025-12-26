// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Menubar } from 'primereact/menubar';
import { IDetailsComponentProps } from 'Components';
import { EventTypeRegistration } from 'Api/Events';
import { Register } from 'Api/Events';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import * as faIcons from 'react-icons/fa6';

export const TypeDetails = (props: IDetailsComponentProps<EventTypeRegistration>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isEditMode, setIsEditMode] = useState(false);
    const [schema, setSchema] = useState<any>(() => JSON.parse(props.item.schema));
    const [register] = Register.use();

    const handleEdit = () => {
        setIsEditMode(true);
    };

    const handleCancel = () => {
        setIsEditMode(false);
        setSchema(JSON.parse(props.item.schema));
    };

    const handleSave = async () => {
        register.eventStore = params.eventStore!;
        register.types = [{
            type: props.item.type,
            schema: JSON.stringify(schema, null, 2)
        }];

        await register.execute();
        setIsEditMode(false);
    };

    const menuItems = [
        ...(!isEditMode ? [{
            label: 'Edit',
            icon: <faIcons.FaPencil className='mr-2' />,
            command: handleEdit
        }] : []),
        ...(isEditMode ? [{
            label: 'Save',
            icon: <faIcons.FaCheck className='mr-2' />,
            command: handleSave
        }, {
            label: 'Cancel',
            icon: <faIcons.FaXmark className='mr-2' />,
            command: handleCancel
        }] : [])
    ];

    return (
        <div className="type-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <div className="px-4 py-2">
                <Menubar aria-label="Actions" model={menuItems} />
            </div>
            <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                <pre style={{
                    background: 'var(--surface-ground)',
                    padding: '1rem',
                    borderRadius: '6px',
                    fontSize: '14px',
                    lineHeight: '1.5'
                }}>
                    {JSON.stringify(schema, null, 2)}
                </pre>
            </div>
        </div>
    );
};
