import { Component, Input, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, ChartType, registerables } from 'chart.js';

// Register Chart.js components
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
export class IncomeExpenseChartComponent implements OnChanges, AfterViewInit, OnDestroy {
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
  @Input() incomeColor = 'rgba(72, 187, 120, 0.6)';
  @Input() expenseColor = 'rgba(245, 101, 101, 0.6)';

  private chart?: Chart;
  private viewInitialized = false;

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
  }

  private createChart(): void {
    if (!this.chartCanvas || this.data.length === 0) return;

    this.destroyChart();

    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    const labels = this.data.map(d => d.label);
    const incomeData = this.data.map(d => d.income);
    const expenseData = this.data.map(d => d.expense);

    const config: ChartConfiguration = {
      type: this.chartType as ChartType,
      data: {
        labels: labels,
        datasets: [
          {
            label: this.incomeLabel,
            data: incomeData,
            backgroundColor: this.incomeColor,
            borderColor: this.incomeColor.replace('0.6', '1'),
            borderWidth: 2,
            tension: this.chartType === 'line' ? 0.4 : undefined,
            fill: this.chartType === 'line'
          },
          {
            label: this.expenseLabel,
            data: expenseData,
            backgroundColor: this.expenseColor,
            borderColor: this.expenseColor.replace('0.6', '1'),
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
    this.chart.update();
  }

  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = undefined;
    }
  }
}
