

export interface Tag {
  id: string;
  name: string;
  color?: string;
}

export interface TaskTheme {
  id: string;
  title: string;
  order: number;
  color?: string;
}

export interface Subtask {
  id: string;
  title: string;
  isDone: boolean;
  order: number;
  createdAt: string | Date;
  completedAt?: string | Date | null;
}

export interface Recurrence {
  id: string;
  rule: string;
  createdAt: string | Date;
  lastOccurrence?: string | Date | null;
  nextOccurrence?: string | Date | null;
}

export type TasksStatus = 'Todo' | 'InProgress' | 'Done';
export type TaskPriority = 'None' | 'Low' | 'Medium' | 'High';

export interface Task {
  id: string;
  title: string;
  description?: string | null;
  dueDate?: string | Date | null;
  status: TasksStatus;
  priority: TaskPriority;
  color?: string | null;
  isArchived: boolean;
  createdAt: string | Date;
  updatedAt: string | Date;
  completedAt?: string | Date | null;
  order: number; 
  theme?: TaskTheme | null;
  subtasks: Subtask[];
  tags: Tag[];
  recurrence?: Recurrence | null;
  isOverdue: boolean;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  dueDate?: string; 
  themeId?: string;
  color?: string;
  priority?: TaskPriority;
  tagIds?: string[];
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  dueDate?: string;
  status: TasksStatus;
  priority: TaskPriority;
  color?: string;
  themeId?: string;
  tagIds?: string[];
}

export interface CreateSubtaskRequest { title: string; }
export interface UpdateSubtaskRequest { title: string; isDone: boolean; }

export interface CreateTaskThemeRequest { title: string; color?: string; }
export interface UpdateTaskThemeRequest { title?: string; color?: string; order?: number; }

export interface CreateRecurrenceRequest { rule: string; }

export interface TaskFilter {
  search?: string;
  tagIds?: string[];
  themeIds?: string[];
  status?: TasksStatus;
  priority?: TaskPriority;
  isArchived?: boolean;
  isOverdue?: boolean;
  dueDateOn?: string; 
  dueDateTo?: string;
  page?: number;
  pageSize?: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
