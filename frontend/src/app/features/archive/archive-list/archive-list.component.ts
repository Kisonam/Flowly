import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ArchiveService } from '../../../shared/services/archive.service';
import {
  ArchivedEntity,
  ArchiveQuery,
  EntityType,
  getEntityTypeName,
  getEntityTypeIcon
} from '../../../shared/models/archive.models';

@Component({
  selector: 'app-archive-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './archive-list.component.html',
  styleUrl: './archive-list.component.scss'
})
export class ArchiveListComponent implements OnInit, OnDestroy {
  
  archivedItems: ArchivedEntity[] = [];
  totalCount = 0;
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;

  selectedEntityType: EntityType | null = null;
  searchQuery = '';
  private searchSubject = new Subject<string>();

  loading = false;
  errorMessage = '';

  showDetailModal = false;
  selectedItem: ArchivedEntity | null = null;
  parsedPayload: any = null;

  Object = Object;

  entityTypes = [
    { value: null, label: 'ARCHIVE.FILTERS.TYPES.ALL' },
    { value: EntityType.Note, label: 'ARCHIVE.FILTERS.TYPES.NOTE' },
    { value: EntityType.Task, label: 'ARCHIVE.FILTERS.TYPES.TASK' },
    { value: EntityType.Transaction, label: 'ARCHIVE.FILTERS.TYPES.TRANSACTION' },
    { value: EntityType.Budget, label: 'ARCHIVE.FILTERS.TYPES.BUDGET' },
    { value: EntityType.FinancialGoal, label: 'ARCHIVE.FILTERS.TYPES.GOAL' }
  ];

  private destroy$ = new Subject<void>();
  private translate = inject(TranslateService);

  constructor(private archiveService: ArchiveService) {}

  ngOnInit(): void {
    this.setupSearchDebounce();
    this.loadArchive();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.currentPage = 1;
        this.loadArchive();
      });
  }

  loadArchive(): void {
    this.loading = true;
    this.errorMessage = '';

    const query: ArchiveQuery = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'ArchivedAt',
      sortDirection: 'desc'
    };

    if (this.selectedEntityType !== null) {
      query.entityType = this.selectedEntityType;
    }

    if (this.searchQuery.trim()) {
      query.search = this.searchQuery.trim();
    }

    this.archiveService
      .getArchived(query)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.archivedItems = response.items;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
          this.loading = false;
        },
        error: (err) => {
          console.error('Failed to load archive:', err);
          this.errorMessage = this.translate.instant('ARCHIVE.ERRORS.LOAD_FAILED');
          this.loading = false;
        }
      });
  }

  onSearchChange(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onEntityTypeChange(): void {
    this.currentPage = 1;
    this.loadArchive();
  }

  restore(item: ArchivedEntity): void {
    const typeName = this.translate.instant(`ARCHIVE.FILTERS.TYPES.${this.getEntityTypeKey(item.entityType)}`);
    if (!confirm(this.translate.instant('ARCHIVE.CONFIRM.RESTORE', { type: typeName, title: item.title }))) {
      return;
    }

    this.archiveService
      .restore(item.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Item restored successfully');
          this.loadArchive();
        },
        error: (err) => {
          console.error('❌ Failed to restore item:', err);
          alert(this.translate.instant('ARCHIVE.ERRORS.RESTORE_FAILED'));
        }
      });
  }

  permanentDelete(item: ArchivedEntity): void {
    const typeName = this.translate.instant(`ARCHIVE.FILTERS.TYPES.${this.getEntityTypeKey(item.entityType)}`);
    const confirmMessage = this.translate.instant('ARCHIVE.CONFIRM.DELETE_PERMANENTLY', { type: typeName, title: item.title });

    if (!confirm(confirmMessage)) {
      return;
    }

    if (!confirm(this.translate.instant('ARCHIVE.CONFIRM.DELETE_FINAL'))) {
      return;
    }

    this.archiveService
      .permanentDelete(item.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Item permanently deleted');
          this.loadArchive();
        },
        error: (err) => {
          console.error('❌ Failed to delete item:', err);
          alert(this.translate.instant('ARCHIVE.ERRORS.DELETE_FAILED'));
        }
      });
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadArchive();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadArchive();
    }
  }

  getEntityTypeName(type: EntityType): string {
    return getEntityTypeName(type);
  }

  getEntityTypeKey(type: EntityType): string {
    switch (type) {
      case EntityType.Note: return 'NOTE';
      case EntityType.Task: return 'TASK';
      case EntityType.Transaction: return 'TRANSACTION';
      case EntityType.Budget: return 'BUDGET';
      case EntityType.FinancialGoal: return 'GOAL';
      default: return 'UNKNOWN';
    }
  }

  getEntityTypeIcon(type: EntityType): string {
    return getEntityTypeIcon(type);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString(this.translate.currentLang, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getMetadataDisplay(item: ArchivedEntity): string {
    
    if (item.metadata && Object.keys(item.metadata).length > 0) {
      const entries = Object.entries(item.metadata);
      if (entries.length > 0) {
        const firstEntry = entries[0];
        const key = firstEntry[0];
        const value = firstEntry[1];

        const label = this.formatMetadataLabel(key);
        const formattedValue = this.formatMetadataValue(key, value);

        const displayText = `${label}: ${formattedValue}`;

        if (entries.length > 1) {
          return `${displayText} (+${entries.length - 1})`;
        }

        return displayText;
      }
    }

    if (item.description) {
      const preview = item.description.length > 50
        ? item.description.substring(0, 50) + '...'
        : item.description;
      return preview;
    }

    return '';
  }

  viewDetails(item: ArchivedEntity): void {
    this.selectedItem = item;
    this.showDetailModal = true;

    this.archiveService.getDetail(item.id).subscribe({
      next: (detail) => {
        try {
          this.parsedPayload = JSON.parse(detail.payloadJson);
        } catch (e) {
          console.error('Failed to parse payload JSON:', e);
          this.parsedPayload = { error: this.translate.instant('ARCHIVE.ERRORS.PARSE_FAILED'), raw: detail.payloadJson };
        }
      },
      error: (err) => {
        console.error('Failed to fetch archive detail:', err);
        this.parsedPayload = { error: this.translate.instant('ARCHIVE.ERRORS.DETAIL_LOAD_FAILED') };
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedItem = null;
    this.parsedPayload = null;
  }

  formatMetadataLabel(key: string): string {
    return this.translate.instant(`ARCHIVE.METADATA.LABELS.${key}`);
  }

  formatMetadataValue(key: string, value: any): string {
    if (value === null || value === undefined) {
      return '—';
    }

    if (key === 'Amount' || key === 'Limit' || key === 'TargetAmount' || key === 'CurrentAmount') {
      return new Intl.NumberFormat(this.translate.currentLang, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      }).format(value);
    }

    if (key === 'CharacterCount') {
      return new Intl.NumberFormat(this.translate.currentLang).format(value);
    }

    if (key === 'Status') {

      let statusKey = value;
      if (value === '0' || value === 0) statusKey = 'Todo';
      if (value === '1' || value === 1) statusKey = 'InProgress';
      if (value === '2' || value === 2) statusKey = 'Done';
      return this.translate.instant(`ARCHIVE.METADATA.VALUES.STATUS.${statusKey}`);
    }

    if (key === 'Priority') {
      let priorityKey = value;
      if (value === '0' || value === 0) priorityKey = 'None';
      if (value === '1' || value === 1) priorityKey = 'Low';
      if (value === '2' || value === 2) priorityKey = 'Medium';
      if (value === '3' || value === 3) priorityKey = 'High';
      return this.translate.instant(`ARCHIVE.METADATA.VALUES.PRIORITY.${priorityKey}`);
    }

    if (key === 'Type') {
      let typeKey = value;
      if (value === '0' || value === 0) typeKey = 'Expense';
      if (value === '1' || value === 1) typeKey = 'Income';
      return this.translate.instant(`ARCHIVE.METADATA.VALUES.TYPE.${typeKey}`);
    }

    return String(value);
  }

  getContentSectionTitle(): string {
    if (!this.selectedItem) return this.translate.instant('ARCHIVE.CONTENT_TITLES.DEFAULT');

    const titleMap: Record<EntityType, string> = {
      [EntityType.Note]: 'ARCHIVE.CONTENT_TITLES.NOTE',
      [EntityType.Task]: 'ARCHIVE.CONTENT_TITLES.TASK',
      [EntityType.Transaction]: 'ARCHIVE.CONTENT_TITLES.TRANSACTION',
      [EntityType.Budget]: 'ARCHIVE.CONTENT_TITLES.BUDGET',
      [EntityType.FinancialGoal]: 'ARCHIVE.CONTENT_TITLES.GOAL'
    };

    const key = titleMap[this.selectedItem.entityType];
    return key ? this.translate.instant(key) : this.translate.instant('ARCHIVE.CONTENT_TITLES.DEFAULT');
  }

  getContentFromPayload(): string {
    if (!this.parsedPayload || this.parsedPayload.error) {
      return '';
    }

    if (this.parsedPayload.Markdown) {
      return this.parsedPayload.Markdown;
    }

    if (this.parsedPayload.Content) {
      return this.parsedPayload.Content;
    }
    if (this.parsedPayload.Text) {
      return this.parsedPayload.Text;
    }

    if (this.parsedPayload.Description) {
      return this.parsedPayload.Description;
    }

    if (this.parsedPayload.Notes) {
      return this.parsedPayload.Notes;
    }

    return '';
  }
}
