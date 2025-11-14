import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
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
  imports: [CommonModule, FormsModule],
  templateUrl: './archive-list.component.html',
  styleUrl: './archive-list.component.scss'
})
export class ArchiveListComponent implements OnInit, OnDestroy {
  // Data
  archivedItems: ArchivedEntity[] = [];
  totalCount = 0;
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;

  // Filters
  selectedEntityType: EntityType | null = null;
  searchQuery = '';
  private searchSubject = new Subject<string>();

  // UI State
  loading = false;
  errorMessage = '';

  // Detail modal
  showDetailModal = false;
  selectedItem: ArchivedEntity | null = null;
  parsedPayload: any = null;

  // For template access
  Object = Object;

  // Entity types for filter
  entityTypes = [
    { value: null, label: 'Всі типи' },
    { value: EntityType.Note, label: 'Нотатки' },
    { value: EntityType.Task, label: 'Завдання' },
    { value: EntityType.Transaction, label: 'Транзакції' },
    { value: EntityType.Budget, label: 'Бюджети' },
    { value: EntityType.FinancialGoal, label: 'Фінансові цілі' }
  ];

  private destroy$ = new Subject<void>();

  constructor(private archiveService: ArchiveService) {}

  ngOnInit(): void {
    this.setupSearchDebounce();
    this.loadArchive();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Setup search with debounce
   */
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

  /**
   * Load archived items
   */
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
          this.errorMessage = 'Не вдалося завантажити архів';
          this.loading = false;
        }
      });
  }

  /**
   * Handle search input change
   */
  onSearchChange(): void {
    this.searchSubject.next(this.searchQuery);
  }

  /**
   * Handle entity type filter change
   */
  onEntityTypeChange(): void {
    this.currentPage = 1;
    this.loadArchive();
  }

  /**
   * Restore archived item
   */
  restore(item: ArchivedEntity): void {
    if (!confirm(`Відновити ${this.getEntityTypeName(item.entityType).toLowerCase()} "${item.title}"?`)) {
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
          alert('Не вдалося відновити елемент');
        }
      });
  }

  /**
   * Permanently delete archived item
   */
  permanentDelete(item: ArchivedEntity): void {
    const entityName = this.getEntityTypeName(item.entityType).toLowerCase();
    const confirmMessage = `⚠️ УВАГА!\n\nВи збираєтеся НАЗАВЖДИ видалити ${entityName} "${item.title}".\n\nЦю дію НЕМОЖЛИВО скасувати!\n\nПродовжити?`;

    if (!confirm(confirmMessage)) {
      return;
    }

    // Double confirmation for permanent deletion
    if (!confirm('Ви впевнені? Це остаточне видалення!')) {
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
          alert('Не вдалося видалити елемент');
        }
      });
  }

  /**
   * Go to next page
   */
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadArchive();
    }
  }

  /**
   * Go to previous page
   */
  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadArchive();
    }
  }

  /**
   * Get entity type display name
   */
  getEntityTypeName(type: EntityType): string {
    return getEntityTypeName(type);
  }

  /**
   * Get entity type icon
   */
  getEntityTypeIcon(type: EntityType): string {
    return getEntityTypeIcon(type);
  }

  /**
   * Format date for display
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString('uk-UA', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Get metadata display value
   */
  getMetadataDisplay(item: ArchivedEntity): string {
    // First, try to show metadata
    if (item.metadata && Object.keys(item.metadata).length > 0) {
      const entries = Object.entries(item.metadata);
      if (entries.length > 0) {
        const firstEntry = entries[0];
        const key = firstEntry[0];
        const value = firstEntry[1];

        // Format the label and value using our helper methods
        const label = this.formatMetadataLabel(key);
        const formattedValue = this.formatMetadataValue(key, value);

        const displayText = `${label}: ${formattedValue}`;

        if (entries.length > 1) {
          return `${displayText} (+${entries.length - 1})`;
        }

        return displayText;
      }
    }

    // If no metadata, show description preview
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

    // Fetch full details from backend
    this.archiveService.getDetail(item.id).subscribe({
      next: (detail) => {
        try {
          this.parsedPayload = JSON.parse(detail.payloadJson);
        } catch (e) {
          console.error('Failed to parse payload JSON:', e);
          this.parsedPayload = { error: 'Не вдалося розібрати дані', raw: detail.payloadJson };
        }
      },
      error: (err) => {
        console.error('Failed to fetch archive detail:', err);
        this.parsedPayload = { error: 'Не вдалося завантажити деталі' };
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedItem = null;
    this.parsedPayload = null;
  }

  /**
   * Format metadata label to be user-friendly
   */
  formatMetadataLabel(key: string): string {
    const labelMap: Record<string, string> = {
      'Amount': 'Сума',
      'CurrencyCode': 'Валюта',
      'Type': 'Тип',
      'Limit': 'Ліміт',
      'TargetAmount': 'Цільова сума',
      'CurrentAmount': 'Поточна сума',
      'Category': 'Категорія',
      'Status': 'Статус',
      'Priority': 'Пріоритет',
      'DueDate': 'Термін',
      'CompletedAt': 'Завершено',
      'CharacterCount': 'Символів',
      'GroupId': 'Група'
    };
    return labelMap[key] || key;
  }

  /**
   * Format metadata value to be user-friendly
   */
  formatMetadataValue(key: string, value: any): string {
    if (value === null || value === undefined) {
      return '—';
    }

    // Format currency amounts
    if (key === 'Amount' || key === 'Limit' || key === 'TargetAmount' || key === 'CurrentAmount') {
      return new Intl.NumberFormat('uk-UA', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      }).format(value);
    }

    // Format character count
    if (key === 'CharacterCount') {
      return new Intl.NumberFormat('uk-UA').format(value);
    }

    // Format task status
    if (key === 'Status') {
      const statusMap: Record<string, string> = {
        'Todo': 'До виконання',
        'InProgress': 'В процесі',
        'Done': 'Виконано',
        '0': 'До виконання',
        '1': 'В процесі',
        '2': 'Виконано'
      };
      return statusMap[value] || value;
    }

    // Format task priority
    if (key === 'Priority') {
      const priorityMap: Record<string, string> = {
        'None': 'Немає',
        'Low': 'Низький',
        'Medium': 'Середній',
        'High': 'Високий',
        '0': 'Немає',
        '1': 'Низький',
        '2': 'Середній',
        '3': 'Високий'
      };
      return priorityMap[value] || value;
    }

    // Format transaction type
    if (key === 'Type') {
      const typeMap: Record<string, string> = {
        'Income': 'Дохід',
        'Expense': 'Витрата',
        '0': 'Витрата',
        '1': 'Дохід'
      };
      return typeMap[value] || value;
    }

    return String(value);
  }

  /**
   * Get content section title based on entity type
   */
  getContentSectionTitle(): string {
    if (!this.selectedItem) return 'Вміст';

    const titleMap: Record<EntityType, string> = {
      [EntityType.Note]: 'Текст нотатки',
      [EntityType.Task]: 'Опис завдання',
      [EntityType.Transaction]: 'Примітка',
      [EntityType.Budget]: 'Опис бюджету',
      [EntityType.FinancialGoal]: 'Опис цілі'
    };

    return titleMap[this.selectedItem.entityType] || 'Вміст';
  }

  /**
   * Extract content from payload based on entity type
   */
  getContentFromPayload(): string {
    if (!this.parsedPayload || this.parsedPayload.error) {
      return '';
    }

    // For Notes - check Markdown field first
    if (this.parsedPayload.Markdown) {
      return this.parsedPayload.Markdown;
    }

    // Try to get Content/Text field (alternative for Notes)
    if (this.parsedPayload.Content) {
      return this.parsedPayload.Content;
    }
    if (this.parsedPayload.Text) {
      return this.parsedPayload.Text;
    }

    // Try to get Description field (for Tasks, Budgets, Goals)
    if (this.parsedPayload.Description) {
      return this.parsedPayload.Description;
    }

    // For Transactions, show Notes if available
    if (this.parsedPayload.Notes) {
      return this.parsedPayload.Notes;
    }

    return '';
  }
}
