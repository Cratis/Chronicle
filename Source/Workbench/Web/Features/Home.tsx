// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CardFooter } from 'Components/Common/CardFooter';
import { EventStoreCard } from 'Components/Common/EventStoreCard';
import { HomeViewModel } from './HomeViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { useRelativePath } from '../Utils/useRelativePath';
import css from './Home.module.css';
import { useDialog } from '@cratis/arc.react/dialogs';
import { AddEventStore } from './AddEventStore';
import { Button } from 'primereact/button';
import { ImPlus } from "react-icons/im";

export const Home = withViewModel(HomeViewModel, ({ viewModel }) => {
    const basePath = useRelativePath('event-store');
    const [AddEventStoreDialog, showAddEventStoreDialog] = useDialog(AddEventStore);

    return (
        <div style={{ top: 0, position: 'fixed' }} className='m-4'>
            <h2 className='text-4xl m-3'>Select Event Store</h2>
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
                    <Button className={css.addCard} onClick={() => showAddEventStoreDialog()}>
                        <ImPlus />
                    </Button>
                </div>
            </div>
            <AddEventStoreDialog />
        </div>
    );
});
