import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ProfileComponent } from './components/profile/profile.component';
import { ThemeSettingsComponent } from './components/theme-settings/theme-settings.component';
import { DataExportComponent } from './components/data-export/data-export.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ProfileComponent,
    ThemeSettingsComponent,
    DataExportComponent
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  private route = inject(ActivatedRoute);

  activeTab: 'profile' | 'theme' | 'export' = 'profile';

  readonly tabs = [
    { id: 'profile' as const, label: 'Профіль', icon: 'user' },
    { id: 'theme' as const, label: 'Тема', icon: 'palette' },
    { id: 'export' as const, label: 'Експорт даних', icon: 'download' }
  ];

  ngOnInit(): void {
    // Check if there's a tab parameter in the URL
    this.route.queryParams.subscribe(params => {
      const tab = params['tab'];
      if (tab && this.isValidTab(tab)) {
        this.activeTab = tab;
      }
    });
  }

  setActiveTab(tabId: 'profile' | 'theme' | 'export'): void {
    this.activeTab = tabId;
  }

  private isValidTab(tab: string): tab is 'profile' | 'theme' | 'export' {
    return ['profile', 'theme', 'export'].includes(tab);
  }
}
