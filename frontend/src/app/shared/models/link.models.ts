
export enum LinkEntityType {
  Note = 1,
  Task = 2,
  Transaction = 3
}

export interface EntityPreview {
  type: LinkEntityType;
  id: string;
  title: string;
  snippet?: string;
  iconUrl?: string;
}

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

export interface CreateLinkRequest {
  fromType: LinkEntityType;
  fromId: string;
  toType: LinkEntityType;
  toId: string;
}
