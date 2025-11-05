import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Tag } from '../../../features/notes/models/note.models';

@Component({
  selector: 'app-tag-selector',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tag-selector.component.html',
  styleUrls: ['./tag-selector.component.scss']
})
export class TagSelectorComponent {
  @Input() title = 'Tags';
  @Input() tags: Tag[] = [];
  @Input() selectedIds: string[] = [];
  @Output() selectedIdsChange = new EventEmitter<string[]>();

  toggle(tagId: string): void {
    const set = new Set(this.selectedIds);
    if (set.has(tagId)) {
      set.delete(tagId);
    } else {
      set.add(tagId);
    }
    this.selectedIds = Array.from(set);
    this.selectedIdsChange.emit(this.selectedIds);
  }

  isSelected(tagId: string): boolean {
    return this.selectedIds?.includes(tagId) ?? false;
  }
}
