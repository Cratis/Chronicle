// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandTrackerContext, useCommandTracker } from '@aksio/cratis-applications-frontend/commands';
import { DetailsList, IColumn, IObjectWithKey, Selection, TextField } from '@fluentui/react';
import { DebitAccount } from 'API/accounts/debit/DebitAccount';
import { SetDebitAccountName } from 'API/accounts/debit/SetDebitAccountName';
import { useEffect, useState } from 'react';

export interface IDebitAccountsListProps {
    accounts: DebitAccount[];
    selection: Selection<IObjectWithKey>;
}

export const DebitAccountsList = (props: IDebitAccountsListProps) => {
    const [setDebitAccountCommands, setSetDebitAccountCommands] = useState<SetDebitAccountName[]>([]);

    const tracker = useCommandTracker();

    const columns: IColumn[] = [
        {
            key: 'name',
            name: 'Name',
            fieldName: 'name',
            minWidth: 200,
            onRender: (item: DebitAccount, index?: number) => {
                return (
                    <TextField defaultValue={item.name} onChange={(event, newValue) => {
                        setDebitAccountCommands[index!].name = newValue!;
                    }} />
                );
            }
        },
        {
            key: 'balance',
            name: 'Balance',
            fieldName: 'balance',
            minWidth: 200
        }
    ];


    useEffect(() => {
        setSetDebitAccountCommands(props.accounts.map(_ => {
            const command = new SetDebitAccountName();
            command.setInitialValues({
                accountId: _.id,
                name: _.name
            });
            tracker.addCommand(command);
            return command;
        }));
    }, [props.accounts]);

    return (
        <DetailsList columns={columns} items={props.accounts} selection={props.selection} />
    );
};
