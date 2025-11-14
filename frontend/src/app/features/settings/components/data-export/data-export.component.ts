import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

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

  exportData(format: ExportFormat): void {
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
