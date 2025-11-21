import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FinanceStats, MultiCurrencyFinanceStats, CurrencyStats } from '../../models/dashboard.models';

@Component({
  selector: 'app-finance-summary',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './finance-summary.component.html',
  styleUrls: ['./finance-summary.component.scss']
})
export class FinanceSummaryComponent implements OnInit, OnChanges {
  @Input() stats: FinanceStats | null = null;
  @Input() multiCurrencyStats: MultiCurrencyFinanceStats | null = null;

  // Currency filtering
  selectedCurrencies: Set<string> = new Set();
  availableCurrencies: string[] = [];

  // Filtered stats
  filteredStats: CurrencyStats[] = [];

  ngOnInit(): void {
    this.initializeCurrencies();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['multiCurrencyStats']) {
      this.initializeCurrencies();
    }
  }

  private initializeCurrencies(): void {
    if (this.multiCurrencyStats) {
      this.availableCurrencies = this.multiCurrencyStats.availableCurrencies;
      // Select all currencies by default
      this.selectedCurrencies = new Set(this.availableCurrencies);
      this.updateFilteredStats();
    }
  }

  toggleCurrency(currency: string): void {
    if (this.selectedCurrencies.has(currency)) {
      // Don't allow deselecting all currencies
      if (this.selectedCurrencies.size > 1) {
        this.selectedCurrencies.delete(currency);
      }
    } else {
      this.selectedCurrencies.add(currency);
    }
    this.updateFilteredStats();
  }

  isCurrencySelected(currency: string): boolean {
    return this.selectedCurrencies.has(currency);
  }

  private updateFilteredStats(): void {
    if (!this.multiCurrencyStats) {
      this.filteredStats = [];
      return;
    }

    this.filteredStats = this.multiCurrencyStats.byCurrency
      .filter(cs => this.selectedCurrencies.has(cs.currencyCode));
  }

  get topExpenseCategories() {
    return this.stats?.expenseByCategory.slice(0, 5) || [];
  }

  get topIncomeCategories() {
    return this.stats?.incomeByCategory.slice(0, 5) || [];
  }

  getCurrencySymbol(currencyCode: string): string {
    const symbols: { [key: string]: string } = {
      'UAH': '₴',
      'USD': '$',
      'EUR': '€',
      'PLN': 'zł',
      'GBP': '£'
    };
    return symbols[currencyCode] || currencyCode;
  }
}
