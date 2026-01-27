import { Component, Input, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewInit, OnDestroy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, ChartType, registerables } from 'chart.js';
import { Subscription } from 'rxjs';
import { ThemeService } from '../../../../core/services/theme.service';

Chart.register(...registerables);

export interface IncomeExpenseData {
  label: string;
  income: number;
  expense: number;
}

@Component({
  selector: 'app-income-expense-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './income-expense-chart.component.html',
  styleUrl: './income-expense-chart.component.scss'
})
export class IncomeExpenseChartComponent implements OnChanges, AfterViewInit, OnDestroy, OnInit {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  @Input() data: IncomeExpenseData[] = [];
  @Input() chartType: 'bar' | 'line' = 'bar';
  @Input() title = 'Доходи vs Витрати';
  @Input() height = '300px';
  @Input() showLegend = true;
  @Input() showTitle = true;
  @Input() responsive = true;
  @Input() incomeLabel = 'Дохід';
  @Input() expenseLabel = 'Витрати';
  @Input() incomeColor = '';
  @Input() expenseColor = '';

  private chart?: Chart;
  private viewInitialized = false;
  private themeService = inject(ThemeService);
  private themeSubscription?: Subscription;

  ngOnInit(): void {
    this.themeSubscription = this.themeService.currentTheme$.subscribe(() => {
      if (this.chart) {
        this.applyDatasetColors();
        this.chart.update();
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.createChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data'] && this.viewInitialized) {
      this.updateChart();
    }
  }

  ngOnDestroy(): void {
    this.destroyChart();
    this.themeSubscription?.unsubscribe();
  }

  private createChart(): void {
    if (!this.chartCanvas || this.data.length === 0) return;

    this.destroyChart();

    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    const labels = this.data.map(d => d.label);
    const incomeData = this.data.map(d => d.income);
    const expenseData = this.data.map(d => d.expense);

    const incomeColors = this.getIncomeColors();
    const expenseColors = this.getExpenseColors();

    const config: ChartConfiguration = {
      type: this.chartType as ChartType,
      data: {
        labels: labels,
        datasets: [
          {
            label: this.incomeLabel,
            data: incomeData,
            backgroundColor: incomeColors.fill,
            borderColor: incomeColors.border,
            borderWidth: 2,
            tension: this.chartType === 'line' ? 0.4 : undefined,
            fill: this.chartType === 'line'
          },
          {
            label: this.expenseLabel,
            data: expenseData,
            backgroundColor: expenseColors.fill,
            borderColor: expenseColors.border,
            borderWidth: 2,
            tension: this.chartType === 'line' ? 0.4 : undefined,
            fill: this.chartType === 'line'
          }
        ]
      },
      options: {
        responsive: this.responsive,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: this.showLegend,
            position: 'top',
          },
          title: {
            display: this.showTitle,
            text: this.title,
            font: {
              size: 16,
              weight: 'bold'
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            callbacks: {
              label: (context) => {
                const label = context.dataset.label || '';
                const value = context.parsed.y || 0;
                return `${label}: ${value.toFixed(2)}`;
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: function(value) {
                return value.toLocaleString();
              }
            }
          },
          x: {
            grid: {
              display: false
            }
          }
        },
        interaction: {
          mode: 'index',
          intersect: false,
        }
      }
    };

    this.chart = new Chart(ctx, config);
  }

  private updateChart(): void {
    if (!this.chart) {
      this.createChart();
      return;
    }

    const labels = this.data.map(d => d.label);
    const incomeData = this.data.map(d => d.income);
    const expenseData = this.data.map(d => d.expense);

    this.chart.data.labels = labels;
    this.chart.data.datasets[0].data = incomeData;
    this.chart.data.datasets[1].data = expenseData;
    this.applyDatasetColors();
    this.chart.update();
  }

  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = undefined;
    }
  }

  private applyDatasetColors(): void {
    if (!this.chart) return;
    const incomeColors = this.getIncomeColors();
    const expenseColors = this.getExpenseColors();

    if (this.chart.data.datasets[0]) {
      this.chart.data.datasets[0].backgroundColor = incomeColors.fill;
      this.chart.data.datasets[0].borderColor = incomeColors.border;
    }

    if (this.chart.data.datasets[1]) {
      this.chart.data.datasets[1].backgroundColor = expenseColors.fill;
      this.chart.data.datasets[1].borderColor = expenseColors.border;
    }
  }

  private getIncomeColors() {
    const border = this.incomeColor || this.getCssVar('--success', '#48bb78');
    const fill = this.getCssVar('--success-light', '#c6f6d5');
    return { border, fill };
  }

  private getExpenseColors() {
    const border = this.expenseColor || this.getCssVar('--danger', '#f56565');
    const fill = this.getCssVar('--danger-light', '#fed7d7');
    return { border, fill };
  }

  private getCssVar(name: string, fallback: string): string {
    if (typeof window === 'undefined') {
      return fallback;
    }
    const value = getComputedStyle(document.documentElement).getPropertyValue(name);
    return value?.trim() || fallback;
  }
}
