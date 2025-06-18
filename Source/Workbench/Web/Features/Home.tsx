// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CardFooter } from 'Components/Common/CardFooter';
import { EventStoreCard } from 'Components/Common/EventStoreCard';
import { HomeViewModel } from './HomeViewModel';
import { withViewModel } from '@cratis/applications.react.mvvm';
import { useRelativePath } from '../Utils/useRelativePath';
import css from './Home.module.css';
import { useDialog } from '@cratis/applications.react/dialogs';
import { AddEventStore } from './AddEventStore';
import { Button } from 'primereact/button';
import { ImPlus } from "react-icons/im";

export const Home = withViewModel(HomeViewModel, ({ viewModel }) => {
    const basePath = useRelativePath('event-store');
    const [AddEventStoreDialog, showAddEventStoreDialog] = useDialog(AddEventStore);

    return (
        <div>
            <main>
                <h2 className='text-4xl m-3'>Select Event Store</h2>
                <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 '>
                    {viewModel.eventStores.map((eventStore) => {
                        return (
                            <EventStoreCard
                                key={eventStore}
                                title={eventStore}
                                path={`${basePath}/${eventStore}/Default`}
                                footer={<CardFooter />}
                                description=''
                            />
                        );
                    })}

                    <div className='m-4 flex'>
                        <Button className={css.addCard} onClick={() => showAddEventStoreDialog()}>
                            <ImPlus/>
                        </Button>
                    </div>
                </div>
            </main>
            <AddEventStoreDialog/>
        </div>
    );
});
