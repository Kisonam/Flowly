export type TransactionType = 'Income' | 'Expense';

export interface TransactionListItem {
  id: string;
  amount: number;
  currencyCode: string;
  type: TransactionType;
  date: string; // ISO string
  description?: string;
  isArchived: boolean;
}
