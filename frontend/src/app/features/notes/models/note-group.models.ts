export interface NoteGroup {
  id: string;
  title: string;
  order: number;
  color?: string;
}

export interface CreateNoteGroup {
  title: string;
  color?: string;
}

export interface UpdateNoteGroup {
  title?: string;
  color?: string;
  order?: number;
}
