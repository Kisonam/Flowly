import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DialogService } from '../../../../core/services/dialog.service';
import { environment } from '../../../../../environments/environment';

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
  private translate = inject(TranslateService);
  private dialogService = inject(DialogService);
  private readonly apiUrl = environment.apiUrl.replace('/api', '');

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

        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = `flowly-export-${format}.${extension}`;

        if (contentDisposition) {
          const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          if (filenameMatch && filenameMatch[1]) {
            filename = filenameMatch[1].replace(/['"]/g, '');
          }
        }

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
    this.dialogService.confirmTranslated('SETTINGS.EXPORT.DELETE_CONFIRM')
      .subscribe(confirmed => {
        if (!confirmed) return;

        this.dialogService.confirmTranslated('SETTINGS.EXPORT.DELETE_FINAL_CONFIRM')
          .subscribe(doubleConfirmed => {
            if (!doubleConfirmed) return;

            this.isExporting = true;
            this.error = '';
            this.message = '';

            this.http.delete(`${this.apiUrl}/api/user/data`)
              .subscribe({
                next: () => {
                  this.isExporting = false;
                  this.message = this.translate.instant('SETTINGS.EXPORT.DELETE_SUCCESS');
                  setTimeout(() => this.message = '', 3000);
                },
                error: (err) => {
                  console.error('Delete error:', err);
                  this.error = this.translate.instant('SETTINGS.EXPORT.DELETE_ERROR');
                  this.isExporting = false;
                  setTimeout(() => this.error = '', 5000);
                }
              });
          });
      });
  }
}
