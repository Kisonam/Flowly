import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { takeUntil, Subject } from 'rxjs';
import { NotesService } from '../../services/notes.service';
import { Note } from '../../models/note.models';
import { marked } from 'marked';
import { LinkService } from '../../../../shared/services/link.service';
import { Link, LinkEntityType } from '../../../../shared/models/link.models';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-note-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  templateUrl: './note-detail.component.html',
  styleUrls: ['./note-detail.component.scss']
})
export class NoteDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private notesService = inject(NotesService);
  private linkService = inject(LinkService);
  private sanitizer = inject(DomSanitizer);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  noteId!: string;
  note: Note | null = null;
  isLoading = true;
  errorMessage = '';
  safeHtml: SafeHtml | null = null;
  links: Link[] = [];

  // Computed lists based on links
  linkedTasks: any[] = [];
  linkedTransactions: any[] = [];
  linkedNotes: any[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage = this.translate.instant('NOTES.DETAIL.ERRORS.INVALID_ID');
      this.isLoading = false;
      return;
    }
    this.noteId = id;
    this.loadNote();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadNote(): void {
    this.isLoading = true;
    this.notesService.getNoteById(this.noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (note) => {
          this.note = note;
          let html = note.htmlCache && note.htmlCache.trim().length > 0
            ? note.htmlCache
            : marked(note.markdown || '') as string;
          html = this.replaceReferenceTokens(html);
          this.safeHtml = this.sanitizer.bypassSecurityTrustHtml(html);

          // Load links for this note
          this.loadLinks();
        },
        error: (err) => {
          this.errorMessage = err?.message || this.translate.instant('NOTES.DETAIL.ERRORS.LOAD_FAILED');
          this.isLoading = false;
        }
      });
  }

  private loadLinks(): void {
    this.linkService.getLinksForNote(this.noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (links) => {
          this.links = links;
          this.processLinks(links);
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load links:', err);
          this.isLoading = false;
        }
      });
  }

  private processLinks(links: Link[]): void {
    this.linkedTasks = [];
    this.linkedTransactions = [];
    this.linkedNotes = [];

    links.forEach(link => {
      // Get the "other" entity preview
      const preview = link.fromType === LinkEntityType.Note && link.fromId === this.noteId
        ? link.toPreview
        : link.fromPreview;

      if (!preview) return;

      const item = {
        id: preview.id,
        title: preview.title,
        snippet: preview.snippet,
        type: preview.type
      };

      switch (preview.type) {
        case LinkEntityType.Task:
          this.linkedTasks.push(item);
          break;
        case LinkEntityType.Transaction:
          this.linkedTransactions.push(item);
          break;
        case LinkEntityType.Note:
          this.linkedNotes.push(item);
          break;
      }
    });
  }

  onEdit(): void {
    this.router.navigate(['/notes', this.noteId, 'edit']);
  }

  onArchive(): void {
    if (!confirm(this.translate.instant('NOTES.DETAIL.CONFIRM.ARCHIVE'))) return;
    this.notesService.deleteNote(this.noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.router.navigate(['/notes']),
        error: (err) => this.errorMessage = err?.message || this.translate.instant('NOTES.DETAIL.ERRORS.ARCHIVE_FAILED')
      });
  }

  onExport(): void {
    this.notesService.exportMarkdown(this.noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {},
        error: (err) => this.errorMessage = err?.message || this.translate.instant('NOTES.DETAIL.ERRORS.EXPORT_FAILED')
      });
  }

  onRestore(): void {
    if (!confirm(this.translate.instant('NOTES.DETAIL.CONFIRM.RESTORE'))) return;
    this.notesService.restoreNote(this.noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.router.navigate(['/notes']),
        error: (err) => this.errorMessage = err?.message || this.translate.instant('NOTES.DETAIL.ERRORS.RESTORE_FAILED')
      });
  }

  onBack(): void {
    this.router.navigate(['/notes']);
  }

  // Replace [[task:ID|Label]] and [[tx:ID|Label]] tokens with styled pills
  private replaceReferenceTokens(html: string): string {
    const refRegex = /\[\[(task|tx):([A-Za-z0-9\-]{6,})\|?([^\]]*)\]\]/g;
    return html.replace(refRegex, (_match, type: string, id: string, label: string) => {
      const kind = type === 'task' 
        ? this.translate.instant('NOTES.DETAIL.REFS.TASK') 
        : this.translate.instant('NOTES.DETAIL.REFS.TRANSACTION');
      const text = (label && label.trim().length > 0) ? label.trim() : `${kind} ${id.substring(0, 6)}…`;
      const cls = type === 'task' ? 'ref-pill task' : 'ref-pill tx';
      return `<span class="${cls}" data-id="${id}" data-type="${type}"><i class="bi ${type === 'task' ? 'bi-check2-square' : 'bi-cash-coin'}"></i> ${this.escapeHtml(text)}</span>`;
    });
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }
}
