import { Component, Input, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

// Register Chart.js components
Chart.register(...registerables);

export interface BudgetProgressData {
  title: string;
  current: number;
  limit: number;
  color?: string;
}

@Component({
  selector: 'app-budget-progress-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './budget-progress-chart.component.html',
  styleUrl: './budget-progress-chart.component.scss'
})
export class BudgetProgressChartComponent implements OnChanges, AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  @Input() data: BudgetProgressData[] = [];
  @Input() title = 'Прогрес бюджетів';
  @Input() height = '300px';
  @Input() showLegend = true;
  @Input() showTitle = true;
  @Input() responsive = true;
  @Input() showPercentage = true;
  @Input() warningThreshold = 80; // Show warning color when budget usage exceeds this percentage
  @Input() dangerThreshold = 100; // Show danger color when budget usage exceeds this percentage

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

    const labels = this.data.map(d => d.title);
    const currentData = this.data.map(d => d.current);
    const limitData = this.data.map(d => d.limit);
    const colors = this.data.map((d, index) =>
      d.color || this.getColorByProgress(d.current, d.limit)
    );

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Витрачено',
            data: currentData,
            backgroundColor: colors,
            borderColor: colors.map(c => c.replace('0.8', '1')),
            borderWidth: 2,
            borderRadius: 5
          },
          {
            label: 'Ліміт',
            data: limitData,
            backgroundColor: 'rgba(229, 231, 235, 0.5)',
            borderColor: 'rgba(156, 163, 175, 0.5)',
            borderWidth: 1,
            borderRadius: 5
          }
        ]
      },
      options: {
        indexAxis: 'y', // Horizontal bars
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
            callbacks: {
              label: (context) => {
                const label = context.dataset.label || '';
                const value = context.parsed.x || 0;

                if (context.datasetIndex === 0) {
                  // Current dataset
                  const limit = this.data[context.dataIndex].limit;
                  const percentage = limit > 0 ? ((value / limit) * 100).toFixed(1) : '0';
                  return `${label}: ${value.toFixed(2)} (${percentage}%)`;
                } else {
                  // Limit dataset
                  return `${label}: ${value.toFixed(2)}`;
                }
              }
            }
          }
        },
        scales: {
          x: {
            beginAtZero: true,
            stacked: false,
            ticks: {
              callback: function(value) {
                return value.toLocaleString();
              }
            }
          },
          y: {
            stacked: false,
            grid: {
              display: false
            }
          }
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

    const labels = this.data.map(d => d.title);
    const currentData = this.data.map(d => d.current);
    const limitData = this.data.map(d => d.limit);
    const colors = this.data.map((d, index) =>
      d.color || this.getColorByProgress(d.current, d.limit)
    );

    this.chart.data.labels = labels;
    this.chart.data.datasets[0].data = currentData;
    this.chart.data.datasets[0].backgroundColor = colors;
    this.chart.data.datasets[0].borderColor = colors.map(c => c.replace('0.8', '1'));
    this.chart.data.datasets[1].data = limitData;
    this.chart.update();
  }

  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = undefined;
    }
  }

  private getColorByProgress(current: number, limit: number): string {
    if (limit === 0) return 'rgba(156, 163, 175, 0.8)'; // Gray for undefined

    const percentage = (current / limit) * 100;

    if (percentage >= this.dangerThreshold) {
      return 'rgba(239, 68, 68, 0.8)'; // Red - over budget
    } else if (percentage >= this.warningThreshold) {
      return 'rgba(251, 191, 36, 0.8)'; // Yellow/Orange - warning
    } else {
      return 'rgba(34, 197, 94, 0.8)'; // Green - safe
    }
  }

  // Helper method to get progress percentage
  getProgressPercentage(budget: BudgetProgressData): number {
    if (budget.limit === 0) return 0;
    return (budget.current / budget.limit) * 100;
  }

  // Helper method to get remaining amount
  getRemainingAmount(budget: BudgetProgressData): number {
    return Math.max(0, budget.limit - budget.current);
  }
}
