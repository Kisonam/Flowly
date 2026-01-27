
export enum EntityType {
  Note = 'Note',
  Task = 'Task',
  Transaction = 'Transaction',
  Budget = 'Budget',
  FinancialGoal = 'FinancialGoal'
}

export interface ArchivedEntity {
  id: string;
  entityType: EntityType;
  entityId: string;
  archivedAt: string; 
  title: string;
  description?: string;
  metadata?: Record<string, any>;
}

export interface ArchivedEntityDetail extends ArchivedEntity {
  payloadJson: string; 
}

export interface ArchiveQuery {
  entityType?: EntityType;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface ArchiveListResponse {
  items: ArchivedEntity[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export function getEntityTypeName(type: EntityType): string {
  switch (type) {
    case EntityType.Note:
      return 'Нотатка';
    case EntityType.Task:
      return 'Завдання';
    case EntityType.Transaction:
      return 'Транзакція';
    case EntityType.Budget:
      return 'Бюджет';
    case EntityType.FinancialGoal:
      return 'Фінансова ціль';
    default:
      return 'Невідомо';
  }
}

export function getEntityTypeIcon(type: EntityType): string {
  switch (type) {
    case EntityType.Note:
      return 'bi-journal-text';
    case EntityType.Task:
      return 'bi-check-square';
    case EntityType.Transaction:
      return 'bi-cash-coin';
    case EntityType.Budget:
      return 'bi-wallet2';
    case EntityType.FinancialGoal:
      return 'bi-trophy';
    default:
      return 'bi-question-circle';
  }
}
