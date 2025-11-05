export interface Tag {
  id: string;
  name: string;
  color?: string;
}

export interface Note {
  id: string;
  title: string;
  markdown: string;
  htmlCache?: string;
  isArchived: boolean;
  tags: Tag[];
  groupId?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateNoteRequest {
  title: string;
  markdown: string;
  tagIds?: string[];
  groupId?: string;
}

export interface UpdateNoteRequest {
  title?: string;
  markdown?: string;
  tagIds?: string[];
  groupId?: string;
}

export interface NoteFilter {
  search?: string;
  tagIds?: string[];
  isArchived?: boolean;
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
