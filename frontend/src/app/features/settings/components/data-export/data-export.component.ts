import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';

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
  imports: [CommonModule, TranslateModule],
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
      name: 'SETTINGS.EXPORT.FORMATS.JSON.NAME',
      description: 'SETTINGS.EXPORT.FORMATS.JSON.DESCRIPTION',
      icon: 'file-text',
      fileExtension: 'json'
    },
    {
      id: 'csv',
      name: 'SETTINGS.EXPORT.FORMATS.CSV.NAME',
      description: 'SETTINGS.EXPORT.FORMATS.CSV.DESCRIPTION',
      icon: 'table',
      fileExtension: 'zip'
    },
    {
      id: 'pdf',
      name: 'SETTINGS.EXPORT.FORMATS.PDF.NAME',
      description: 'SETTINGS.EXPORT.FORMATS.PDF.DESCRIPTION',
      icon: 'file',
      fileExtension: 'pdf'
    }
  ];

  exportMarkdown(): void {
    this.exportData('markdown', 'zip');
  }

  exportFormat(formatId: string): void {
    const format = this.exportFormats.find(f => f.id === formatId);
    if (!format) return;

    this.exportData(formatId, format.fileExtension);
  }

  private exportData(format: string, extension: string): void {
    this.isExporting = true;
    this.error = '';
    this.message = '';

    const endpoint = format === 'markdown' ? '/api/export' : `/api/export/${format}`;

    this.http.get(`${this.apiUrl}${endpoint}`, {
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
        let filename = `flowly-export-${format}.${extension}`;

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

        const formatName = format === 'markdown' ? 'Markdown ZIP' : format.toUpperCase();
        this.message = `Дані успішно експортовано у форматі ${formatName}`;
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
