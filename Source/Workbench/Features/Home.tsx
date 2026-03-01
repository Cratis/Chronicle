// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CardFooter } from 'Components/Common/CardFooter';
import { EventStoreCard } from 'Components/Common/EventStoreCard';
import { HomeViewModel } from './HomeViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { useRelativePath } from '../Utils/useRelativePath';
import css from './Home.module.css';
import { useState } from 'react';
import { AddEventStore as AddEventStoreCommand } from 'Api/EventStores';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { Button } from 'primereact/button';
import { ImPlus } from "react-icons/im";
import strings from 'Strings';

export const Home = withViewModel(HomeViewModel, ({ viewModel }) => {
    const basePath = useRelativePath('event-store');
    const [showAddEventStore, setShowAddEventStore] = useState(false);

    return (
        <div style={{ top: 0, position: 'fixed' }} className='m-4'>
            <h2 className='text-4xl m-3'>{strings.home.selectHeader}</h2>
            <div className='flex flex-wrap w-full'>
                {viewModel.eventStores.map((eventStore) => {
                    return (
                        <EventStoreCard
                            key={eventStore}
                            title={eventStore}
                            path={`${basePath}/${eventStore}`}
                            footer={<CardFooter />}
                            description=''
                        />
                    );
                })}

                <div className='m-4 flex'>
                    <Button className={css.addCard} onClick={() => setShowAddEventStore(true)}>
                        <ImPlus />
                    </Button>
                </div>
            </div>
            <CommandDialog
                command={AddEventStoreCommand}
                visible={showAddEventStore}
                header={strings.home.dialogs.addEventStore.title}
                confirmLabel={strings.general.buttons.ok}
                cancelLabel={strings.general.buttons.cancel}
                width="20vw"
                onConfirm={result => { if (result.isSuccess) setShowAddEventStore(false); }}
                onCancel={() => setShowAddEventStore(false)}>
                <InputTextField<AddEventStoreCommand>
                    value={c => c.name}
                    title={strings.home.dialogs.addEventStore.name}
                    icon={<i className="pi pi-pencil" />} />
            </CommandDialog>
        </div>
    );
});
