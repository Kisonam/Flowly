export interface TaskReorderItem {
  taskId: string;
  themeId?: string | null;
  order: number;
}

export interface ReorderTasksRequest {
  items: TaskReorderItem[];
}
