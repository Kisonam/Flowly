/**
 * Entity types that can be linked
 * Values must match backend enum: Note = 1, Task = 2, Transaction = 3
 */
export enum LinkEntityType {
  Note = 1,
  Task = 2,
  Transaction = 3
}

/**
 * Preview of an entity for display in link previews
 */
export interface EntityPreview {
  type: LinkEntityType;
  id: string;
  title: string;
  snippet?: string;
  iconUrl?: string;
}

/**
 * Link between two entities
 */
export interface Link {
  id: string;
  fromType: LinkEntityType;
  fromId: string;
  toType: LinkEntityType;
  toId: string;
  fromPreview?: EntityPreview;
  toPreview?: EntityPreview;
  createdAt: Date | string;
}

/**
 * Request to create a new link
 */
export interface CreateLinkRequest {
  fromType: LinkEntityType;
  fromId: string;
  toType: LinkEntityType;
  toId: string;
}
