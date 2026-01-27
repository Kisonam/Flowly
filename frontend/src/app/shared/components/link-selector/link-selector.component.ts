import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, OnInit, signal, computed } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { Link, LinkEntityType, EntityPreview } from '../../models/link.models';
import { LinkService } from '../../services/link.service';
import { NotesService } from '../../../features/notes/services/notes.service';
import { TasksService } from '../../services/tasks.service';
import { TransactionsService } from '../../services/transactions.service';
import { debounceTime, distinctUntilChanged, Subject, switchMap, catchError, of } from 'rxjs';

@Component({
  selector: 'app-link-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './link-selector.component.html',
  styleUrls: ['./link-selector.component.scss']
})
export class LinkSelectorComponent implements OnInit {
  private linkService = inject(LinkService);
  private notesService = inject(NotesService);
  private tasksService = inject(TasksService);
  private transactionsService = inject(TransactionsService);
  private translate = inject(TranslateService);

  @Input({ required: true }) entityType!: LinkEntityType;
  @Input({ required: true }) entityId!: string;
  @Input() title = 'Links';
  @Input() showAddButton = true;

  @Output() linkCreated = new EventEmitter<Link>();
  @Output() linkDeleted = new EventEmitter<string>();

  links = signal<Link[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);

  showAddDialog = signal(false);
  selectedLinkType = signal<LinkEntityType>(LinkEntityType.Note);
  searchQuery = signal('');
  searchResults = signal<any[]>([]);
  isSearching = signal(false);

  private searchSubject = new Subject<string>();

  LinkEntityType = LinkEntityType;

  availableLinkTypes = computed(() => {
    return Object.values(LinkEntityType)
      .filter((value): value is LinkEntityType => typeof value === 'number')
      .filter(type => type !== this.entityType);
  });

  linkTypeOptions = [
    { value: LinkEntityType.Note, label: 'Note', icon: 'bi-journal-text' },
    { value: LinkEntityType.Task, label: 'Task', icon: 'bi-check2-square' },
    { value: LinkEntityType.Transaction, label: 'Transaction', icon: 'bi-currency-dollar' }
  ];

  ngOnInit(): void {
    this.loadLinks();
    this.setupSearch();
  }

  loadLinks(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.linkService.getLinksForEntity(this.entityType, this.entityId)
      .subscribe({
        next: (links) => {
          this.links.set(links);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.isLoading.set(false);
        }
      });
  }

  private setupSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query || query.trim().length < 2) {
          this.searchResults.set([]);
          return of([]);
        }

        this.isSearching.set(true);
        return this.performSearch(query.trim());
      })
    ).subscribe({
      next: (results) => {
        this.searchResults.set(results);
        this.isSearching.set(false);
      },
      error: () => {
        this.isSearching.set(false);
      }
    });
  }

  private performSearch(query: string) {
    const type = this.selectedLinkType();

    switch (type) {
      case LinkEntityType.Note:
        return this.notesService.getNotes({ search: query, isArchived: false, pageSize: 10 }).pipe(
          switchMap(result => of(result.items)),
          catchError(() => of([]))
        );

      case LinkEntityType.Task:
        return this.tasksService.list({ search: query, isArchived: false, take: 10 }).pipe(
          catchError(() => of([]))
        );

      case LinkEntityType.Transaction:
        return this.transactionsService.list({ search: query, isArchived: false, take: 10 }).pipe(
          catchError(() => of([]))
        );

      default:
        return of([]);
    }
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.searchSubject.next(query);
  }

  onLinkTypeChange(type: LinkEntityType): void {
    this.selectedLinkType.set(type);
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  openAddDialog(): void {
    this.showAddDialog.set(true);
    
    const available = this.availableLinkTypes();
    if (available.length > 0) {
      this.selectedLinkType.set(available[0]);
    }
  }

  closeAddDialog(): void {
    this.showAddDialog.set(false);
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  createLink(targetEntity: any): void {
    const request = {
      fromType: this.entityType,
      fromId: this.entityId,
      toType: this.selectedLinkType(),
      toId: targetEntity.id
    };

    this.linkService.createLink(request)
      .subscribe({
        next: (link) => {
          this.links.update(links => [...links, link]);
          this.linkCreated.emit(link);
          this.closeAddDialog();
        },
        error: (err) => {
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_CREATE'));
        }
      });
  }

  deleteLink(link: Link): void {
    if (!confirm(this.translate.instant('COMMON.CONFIRM.REMOVE_LINK'))) {
      return;
    }

    this.linkService.deleteLink(link.id)
      .subscribe({
        next: () => {
          this.links.update(links => links.filter(l => l.id !== link.id));
          this.linkDeleted.emit(link.id);
        },
        error: (err) => {
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_DELETE'));
        }
      });
  }

  getLinkPreview(link: Link): EntityPreview | undefined {
    
    if (link.fromType === this.entityType && link.fromId === this.entityId) {
      return link.toPreview;
    }
    
    return link.fromPreview;
  }

  getEntityIcon(type: LinkEntityType): string {
    switch (type) {
      case LinkEntityType.Note:
        return 'bi-journal-text';
      case LinkEntityType.Task:
        return 'bi-check2-square';
      case LinkEntityType.Transaction:
        return 'bi-currency-dollar';
      default:
        return 'bi-link-45deg';
    }
  }

  getEntityTypeName(type: LinkEntityType): string {
    switch (type) {
      case LinkEntityType.Note:
        return 'Note';
      case LinkEntityType.Task:
        return 'Task';
      case LinkEntityType.Transaction:
        return 'Transaction';
      default:
        return 'Unknown';
    }
  }

  getResultTitle(result: any): string {
    return result.title || result.name || 'Untitled';
  }

  getResultSubtitle(result: any): string {
    const type = this.selectedLinkType();

    switch (type) {
      case LinkEntityType.Note:
        return result.markdown?.substring(0, 100) || 'No content';

      case LinkEntityType.Task:
        return result.description?.substring(0, 100) || 'No description';

      case LinkEntityType.Transaction:
        return `${result.amount} ${result.currencyCode} - ${result.type}`;

      default:
        return '';
    }
  }
}
