import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FinanceStats } from '../../models/dashboard.models';

@Component({
  selector: 'app-finance-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './finance-summary.component.html',
  styleUrls: ['./finance-summary.component.scss']
})
export class FinanceSummaryComponent {
  @Input() stats: FinanceStats | null = null;

  get topExpenseCategories() {
    return this.stats?.expenseByCategory.slice(0, 5) || [];
  }

  get topIncomeCategories() {
    return this.stats?.incomeByCategory.slice(0, 5) || [];
  }
}
