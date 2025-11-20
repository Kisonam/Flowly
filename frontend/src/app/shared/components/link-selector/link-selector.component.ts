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

/**
 * Link Selector Component
 *
 * Allows searching and selecting entities (Notes, Tasks, Transactions) to create links,
 * and displays current links with the ability to delete them.
 *
 * @example
 * <app-link-selector
 *   [entityType]="LinkEntityType.Note"
 *   [entityId]="noteId"
 *   (linkCreated)="onLinkCreated($event)"
 *   (linkDeleted)="onLinkDeleted($event)">
 * </app-link-selector>
 */
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

  // Inputs
  @Input({ required: true }) entityType!: LinkEntityType;
  @Input({ required: true }) entityId!: string;
  @Input() title = 'Links';
  @Input() showAddButton = true;

  // Outputs
  @Output() linkCreated = new EventEmitter<Link>();
  @Output() linkDeleted = new EventEmitter<string>();

  // State
  links = signal<Link[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);

  // Add link dialog state
  showAddDialog = signal(false);
  selectedLinkType = signal<LinkEntityType>(LinkEntityType.Note);
  searchQuery = signal('');
  searchResults = signal<any[]>([]);
  isSearching = signal(false);

  // Search subject for debouncing
  private searchSubject = new Subject<string>();

  // Expose LinkEntityType to template
  LinkEntityType = LinkEntityType;

  // Available link types (excluding current entity type)
  availableLinkTypes = computed(() => {
    return Object.values(LinkEntityType)
      .filter((value): value is LinkEntityType => typeof value === 'number')
      .filter(type => type !== this.entityType);
  });

  // Link type options for dropdown
  linkTypeOptions = [
    { value: LinkEntityType.Note, label: 'Note', icon: 'bi-journal-text' },
    { value: LinkEntityType.Task, label: 'Task', icon: 'bi-check2-square' },
    { value: LinkEntityType.Transaction, label: 'Transaction', icon: 'bi-currency-dollar' }
  ];

  ngOnInit(): void {
    this.loadLinks();
    this.setupSearch();
  }

  /**
   * Load all links for the current entity
   */
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

  /**
   * Setup search with debouncing
   */
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

  /**
   * Perform search based on selected link type
   */
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

  /**
   * Handle search query change
   */
  onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.searchSubject.next(query);
  }

  /**
   * Handle link type change
   */
  onLinkTypeChange(type: LinkEntityType): void {
    this.selectedLinkType.set(type);
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  /**
   * Open add link dialog
   */
  openAddDialog(): void {
    this.showAddDialog.set(true);
    // Set default link type to first available type
    const available = this.availableLinkTypes();
    if (available.length > 0) {
      this.selectedLinkType.set(available[0]);
    }
  }

  /**
   * Close add link dialog
   */
  closeAddDialog(): void {
    this.showAddDialog.set(false);
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  /**
   * Create a link to selected entity
   */
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

  /**
   * Delete a link
   */
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

  /**
   * Get the preview to display for a link (the "other" entity)
   */
  getLinkPreview(link: Link): EntityPreview | undefined {
    // If current entity is the "from", show "to" preview
    if (link.fromType === this.entityType && link.fromId === this.entityId) {
      return link.toPreview;
    }
    // Otherwise show "from" preview
    return link.fromPreview;
  }

  /**
   * Get icon class for entity type
   */
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

  /**
   * Get display name for entity type
   */
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

  /**
   * Get display title for search result
   */
  getResultTitle(result: any): string {
    return result.title || result.name || 'Untitled';
  }

  /**
   * Get display subtitle/snippet for search result
   */
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
