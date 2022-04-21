// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import { AllProjections, AllProjectionsArguments } from 'API/events/projections/AllProjections';
import { Projection } from 'API/events/projections/Projection';

import {
    DetailsList,
    Dropdown,
    IColumn,
    IDetailsListStyles,
    IDropdownOption,
    IDropdownStyles,
    Pivot,
    PivotItem,
    Selection,
    SelectionMode,
    Stack
} from '@fluentui/react';
import { Collections } from './Collections';
import { useEffect } from 'react';
import { Microservices } from 'API/configuration/Microservices';
import { Microservice } from 'API/configuration/Microservice';

const gridStyles: Partial<IDetailsListStyles> = {
    root: {
        height: '100%',
        overflowX: 'scroll',
        selectors: {
            '& [role=grid]': {
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'start'
            },
        },
    },
    headerWrapper: {
        flex: '0 0 auto',
    },
    contentWrapper: {
        flex: '1 1 auto',
        overflowX: 'hidden',
        overflowY: 'auto',
        height: '300px'
    },
};

const columns: IColumn[] = [
    {
        key: 'id',
        name: 'Id',
        fieldName: 'id',
        minWidth: 250,
        maxWidth: 250,
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200,
        isResizable: true
    },
    {
        key: 'model-name',
        name: 'Model Name',
        fieldName: 'modelName',
        minWidth: 200,
        isResizable: true
    },
];

const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

export const Projections = () => {
    const [microservices] = Microservices.use();
    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();

    const getAllProjectionsArguments = () => {
        return {
            microserviceId: selectedMicroservice?.id || undefined!
        } as AllProjectionsArguments;
    }

    const [projections, refreshProjections] = AllProjections.use(getAllProjectionsArguments());
    const [selected, setSelected] = useState<Projection>();

    const microserviceOptions = microservices.data.map(_ => {
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
        if (selectedMicroservice) {
            refreshProjections(getAllProjectionsArguments());
        }
    }, [selectedMicroservice]);

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelected(selected[0] as Projection);
                } else {
                    setSelected(undefined);
                }
            },
        }), [projections.data]);

    return (
        <Stack style={{ height: '100%' }}>
            <Stack.Item>
                <Dropdown
                    styles={commandBarDropdownStyles}
                    options={microserviceOptions}
                    selectedKey={selectedMicroservice?.id}
                    onChange={(e, option) => {
                        setSelectedMicroservice(microservices.data.find(_ => _.id == option!.key));
                    }} />

            </Stack.Item>
            <Stack.Item grow={1}>
                <DetailsList
                    items={projections.data}
                    columns={columns}
                    selection={selection}
                    styles={gridStyles} />
            </Stack.Item>
            {selected &&
                <Stack.Item grow={1}>
                    <Pivot linkFormat="links">
                        <PivotItem headerText="Collections">
                            <Collections projectionId={selected.id} />
                        </PivotItem>
                    </Pivot>
                </Stack.Item>
            }
        </Stack >
    );
};
