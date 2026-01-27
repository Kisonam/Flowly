import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NotesService } from '../../../notes/services/notes.service';
import { Note } from '../../../notes/models/note.models';

interface ThemeColumn {
  id: string;
  name: string;
  color: string;
  order: number;
  noteIds: string[];
}

@Component({
  selector: 'app-themes-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './themes-board.component.html',
  styleUrls: ['./themes-board.component.scss']
})
export class ThemesBoardComponent implements OnInit {
  private notesService = inject(NotesService);

  notes: Note[] = [];
  themes: ThemeColumn[] = [];
  newThemeName = '';

  ngOnInit(): void {
    this.loadThemes();
    this.loadNotes();
  }

  private loadNotes(): void {
    
    this.notesService.getNotes({ isArchived: false, page: 1, pageSize: 100 })
      .subscribe({
        next: (res) => this.notes = res.items,
        error: () => this.notes = []
      });
  }

  private loadThemes(): void {
    try {
      const raw = localStorage.getItem('flowly_themes');
      this.themes = raw ? JSON.parse(raw) : [];
    } catch {
      this.themes = [];
    }
  }

  private saveThemes(): void {
    localStorage.setItem('flowly_themes', JSON.stringify(this.themes));
  }

  addTheme(): void {
    const name = this.newThemeName.trim();
    if (!name) return;
    const theme: ThemeColumn = {
      id: crypto.randomUUID(),
      name,
      color: this.pickColor(),
      order: this.themes.length,
      noteIds: []
    };
    this.themes.push(theme);
    this.newThemeName = '';
    this.saveThemes();
  }

  removeTheme(themeId: string): void {
    if (!confirm('Видалити тему? Нотатки залишаться без теми.')) return;
    this.themes = this.themes.filter(t => t.id !== themeId);
    this.saveThemes();
  }

  onDragStartNote(ev: DragEvent, noteId: string): void {
    ev.dataTransfer?.setData('text/plain', noteId);
    ev.dataTransfer?.setDragImage(new Image(), 0, 0);
  }

  allowDrop(ev: DragEvent): void { ev.preventDefault(); }

  onDropToTheme(ev: DragEvent, theme: ThemeColumn): void {
    ev.preventDefault();
    const noteId = ev.dataTransfer?.getData('text/plain');
    if (!noteId) return;

    for (const col of this.themes) {
      const idx = col.noteIds.indexOf(noteId);
      if (idx > -1) col.noteIds.splice(idx, 1);
    }

    if (!theme.noteIds.includes(noteId)) theme.noteIds.push(noteId);
    this.saveThemes();
  }

  moveTheme(theme: ThemeColumn, dir: -1 | 1): void {
    const idx = this.themes.indexOf(theme);
    const newIdx = idx + dir;
    if (newIdx < 0 || newIdx >= this.themes.length) return;
    const [removed] = this.themes.splice(idx, 1);
    this.themes.splice(newIdx, 0, removed);
    this.saveThemes();
  }

  getNoteById(id: string): Note | undefined {
    return this.notes.find(n => n.id === id);
  }

  private pickColor(): string {
    const palette = ['#8b5cf6', '#ef4444', '#10b981', '#3b82f6', '#f59e0b', '#ec4899'];
    return palette[this.themes.length % palette.length];
  }
}
