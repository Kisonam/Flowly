/**
 * Entity types that can be archived
 */
export enum EntityType {
  Note = 'Note',
  Task = 'Task',
  Transaction = 'Transaction',
  Budget = 'Budget',
  FinancialGoal = 'FinancialGoal'
}

/**
 * Archived entity item
 */
export interface ArchivedEntity {
  id: string;
  entityType: EntityType;
  entityId: string;
  archivedAt: string; // ISO date string
  title: string;
  description?: string;
  metadata?: Record<string, any>;
}

/**
 * Archived entity detail (includes full JSON payload)
 */
export interface ArchivedEntityDetail extends ArchivedEntity {
  payloadJson: string; // Full JSON snapshot of the archived entity
}

/**
 * Query parameters for archived entities
 */
export interface ArchiveQuery {
  entityType?: EntityType;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

/**
 * Paginated response for archived entities
 */
export interface ArchiveListResponse {
  items: ArchivedEntity[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Helper function to get entity type display name
 */
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

/**
 * Helper function to get entity type icon
 */
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
