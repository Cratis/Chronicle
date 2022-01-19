import { useMemo, useState } from 'react';
import { useDialog, DialogResult } from '@aksio/frontend/dialogs';
import { CreateAccountDialog, CreateAccountDialogResult } from './CreateAccountDialog';
import { Guid } from '@cratis/fundamentals';

import {
    CommandBar,
    IColumn,
    ICommandBarItemProps,
    DetailsList,
    Selection,
    SelectionMode,
    Stack,
    SearchBox
} from '@fluentui/react';
import { AmountDialog, AmountDialogInput, AmountDialogResult } from './AmountDialog';
import { CreateDebitAccount } from './CreateDebitAccount';
import { DepositToAccount } from './DepositToAccount';
import { WithdrawFromAccount } from './WithdrawFromAccount';
import { AllAccounts } from './AllAccounts';
import { StartingWith } from './StartingWith';

const columns: IColumn[] = [
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    },
    {
        key: 'balance',
        name: 'Balance',
        fieldName: 'balance',
        minWidth: 200
    }
];

export const DebitAccounts = () => {
    const [accounts, queryAccounts] = AllAccounts.use();
    const [accountsStartingWith, queryAccountsStartingWith] = StartingWith.use({ filter: '' });
    const [searching, setSearching] = useState<boolean>(false);
    const [selectedItem, setSelectedItem] = useState<any>(undefined);
    const [showCreateAccount, createAccountDialogProps] = useDialog<any, CreateAccountDialogResult>(async (result, output?) => {
        if (result === DialogResult.Success && output) {
            const command = new CreateDebitAccount();
            command.accountId = Guid.create().toString(),
                command.name = output.name;
            command.owner = 'edd60145-a6df-493f-b48d-35ffdaaefc4c';
            await command.execute();
            setTimeout(queryAccounts, 20);
        }
    });


    const [showDepositAmountDialog, depositAmountDialogProps] = useDialog<AmountDialogInput, AmountDialogResult>(async (result, output?) => {
        if (result === DialogResult.Success && output && selectedItem) {
            const command = new DepositToAccount();
            command.accountId = selectedItem.id;
            command.amount = output.amount;
            await command.execute();
            setTimeout(queryAccounts, 20);
        }
    });

    const [showWithdrawAmountDialog, withdrawAmountDialogProps] = useDialog<AmountDialogInput, AmountDialogResult>(async (result, output?) => {
        if (result === DialogResult.Success && output && selectedItem) {
            const command = new WithdrawFromAccount();
            command.accountId = selectedItem.id;
            command.amount = output.amount;
            await command.execute();
            setTimeout(queryAccounts, 20);
        }
    });

    const searchFor = (filter: string) => {
        if (filter && filter !== '') {
            setSearching(true);
        } else {
            setSearching(false);
        }
        queryAccountsStartingWith({ filter });
    };

    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'add',
            name: 'Add Debit Account',
            iconProps: { iconName: 'Add' },
            onClick: showCreateAccount
        },
        {
            key: 'refresh',
            name: 'Refresh',
            iconProps: { iconName: 'Refresh' },
            onClick: () => queryAccounts()
        },
        {
            key: 'search',
            onRender: (props, defaultRenderer) => {
                return (
                    <div style={{ position: 'relative', top: '6px' }}>
                        <SearchBox
                            placeholder="Accounts starting with"
                            onClear={() => searchFor('')}
                            onChange={(ev, newValue) => searchFor(newValue || '')} />
                    </div>
                );
            }
        }
    ];
    

    if (selectedItem) {
        commandBarItems.push(
            {
                key: 'deposit',
                name: 'Deposit',
                iconProps: { iconName: 'Money' },
                onClick: () => showDepositAmountDialog({ okTitle: 'Deposit' })
            }
        );

        commandBarItems.push(
            {
                key: 'withdraw',
                name: 'Withdraw',
                iconProps: { iconName: 'Money' },
                onClick: () => showWithdrawAmountDialog({ okTitle: 'Withdraw' })
            }
        );
    }

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelectedItem(selected[0]);
                }
            },
            items: accounts.data as any
        }), [accounts.data]);

    const items = searching ? accountsStartingWith.data : accounts.data;

    return (
        <>
            <Stack>
                <Stack.Item disableShrink>
                    <CommandBar items={commandBarItems} />
                </Stack.Item>
                <Stack.Item>
                    <DetailsList columns={columns} items={items} selection={selection} />
                </Stack.Item>
            </Stack>

            <CreateAccountDialog {...createAccountDialogProps} />
            <AmountDialog {...depositAmountDialogProps} />
            <AmountDialog {...withdrawAmountDialogProps} />
        </>
    );
};