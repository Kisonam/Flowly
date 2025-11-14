import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

interface ExportFormat {
  id: string;
  name: string;
  description: string;
  icon: string;
  fileExtension: string;
}

@Component({
  selector: 'app-data-export',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './data-export.component.html',
  styleUrls: ['./data-export.component.scss']
})
export class DataExportComponent {
  private http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000'; // TODO: Use environment config

  isExporting = false;
  message = '';
  error = '';

  readonly exportFormats: ExportFormat[] = [
    {
      id: 'json',
      name: 'JSON',
      description: 'Експорт даних у форматі JSON',
      icon: 'file-text',
      fileExtension: 'json'
    },
    {
      id: 'csv',
      name: 'CSV',
      description: 'Експорт даних у форматі CSV (для Excel)',
      icon: 'table',
      fileExtension: 'csv'
    },
    {
      id: 'pdf',
      name: 'PDF',
      description: 'Експорт звіту у форматі PDF',
      icon: 'file',
      fileExtension: 'pdf'
    }
  ];

  exportMarkdown(): void {
    this.isExporting = true;
    this.error = '';
    this.message = '';

    this.http.get(`${this.apiUrl}/api/export`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) {
          this.error = 'Помилка завантаження файлу';
          this.isExporting = false;
          return;
        }

        // Extract filename from Content-Disposition header or use default
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = 'flowly-notes-export.zip';

        if (contentDisposition) {
          const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          if (filenameMatch && filenameMatch[1]) {
            filename = filenameMatch[1].replace(/['"]/g, '');
          }
        }

        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.click();

        window.URL.revokeObjectURL(url);

        this.message = 'Нотатки успішно експортовано у Markdown';
        this.isExporting = false;

        setTimeout(() => this.message = '', 5000);
      },
      error: (err) => {
        console.error('Export error:', err);
        this.error = 'Помилка при експорті даних. Спробуйте пізніше.';
        this.isExporting = false;

        setTimeout(() => this.error = '', 5000);
      }
    });
  }  exportData(format: ExportFormat): void {
    this.isExporting = true;
    this.error = '';
    this.message = '';

    // TODO: Implement actual export functionality
    setTimeout(() => {
      this.isExporting = false;
      this.message = `Дані успішно експортовано у форматі ${format.name}`;

      // Simulate file download
      console.log(`Exporting data as ${format.fileExtension}`);

      setTimeout(() => this.message = '', 5000);
    }, 1500);
  }

  deleteAllData(): void {
    const confirmed = confirm(
      'Ви впевнені, що хочете видалити всі дані? Цю дію неможливо скасувати!'
    );

    if (!confirmed) {
      return;
    }

    const doubleConfirmed = confirm(
      'ОСТАННЯ ПОПЕРЕДЖЕННЯ: Всі ваші дані будуть безповоротно видалені. Продовжити?'
    );

    if (!doubleConfirmed) {
      return;
    }

    this.isExporting = true;
    this.error = '';
    this.message = '';

    // TODO: Implement actual data deletion
    setTimeout(() => {
      this.isExporting = false;
      this.message = 'Всі дані видалено';
      setTimeout(() => this.message = '', 3000);
    }, 1000);
  }
}
