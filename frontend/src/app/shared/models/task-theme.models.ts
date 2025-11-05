export interface TaskTheme {
  id: string;
  title: string;
  order: number;
  color?: string;
}

export interface CreateTaskTheme {
  title: string;
  color?: string;
}

export interface UpdateTaskTheme {
  title?: string;
  color?: string;
  order?: number;
}
