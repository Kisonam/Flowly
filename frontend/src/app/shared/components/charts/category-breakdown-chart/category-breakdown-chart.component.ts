import { Component, Input, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration, ChartType, registerables } from 'chart.js';

Chart.register(...registerables);

export interface CategoryBreakdownData {
  categoryName: string;
  amount: number;
  color?: string;
}

@Component({
  selector: 'app-category-breakdown-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-breakdown-chart.component.html',
  styleUrl: './category-breakdown-chart.component.scss'
})
export class CategoryBreakdownChartComponent implements OnChanges, AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  @Input() data: CategoryBreakdownData[] = [];
  @Input() chartType: 'pie' | 'doughnut' = 'doughnut';
  @Input() title = 'Розподіл по категоріях';
  @Input() height = '300px';
  @Input() showLegend = true;
  @Input() showTitle = true;
  @Input() responsive = true;
  @Input() legendPosition: 'top' | 'bottom' | 'left' | 'right' = 'right';
  @Input() showPercentage = true;

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

    const labels = this.data.map(d => d.categoryName);
    const dataValues = this.data.map(d => d.amount);
    const colors = this.data.map((d, index) =>
      d.color || this.generateColor(index)
    );

    const config: ChartConfiguration = {
      type: this.chartType as ChartType,
      data: {
        labels: labels,
        datasets: [{
          data: dataValues,
          backgroundColor: colors,
          borderWidth: 2,
          borderColor: '#fff',
          hoverOffset: 4
        }]
      },
      options: {
        responsive: this.responsive,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: this.showLegend,
            position: this.legendPosition,
            labels: {
              padding: 15,
              font: {
                size: 12
              },
              generateLabels: (chart) => {
                const data = chart.data;
                if (data.labels && data.datasets.length) {
                  const dataset = data.datasets[0];
                  const total = (dataset.data as number[]).reduce((a, b) => a + b, 0);

                  return data.labels.map((label, i) => {
                    const value = (dataset.data as number[])[i];
                    const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0';

                    return {
                      text: this.showPercentage ? `${label} (${percentage}%)` : label as string,
                      fillStyle: (dataset.backgroundColor as string[])[i],
                      hidden: false,
                      index: i
                    };
                  });
                }
                return [];
              }
            }
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
                const label = context.label || '';
                const value = context.parsed || 0;
                const total = (context.dataset.data as number[]).reduce((a, b) => a + b, 0);
                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
                return `${label}: ${value.toFixed(2)} (${percentage}%)`;
              }
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

    const labels = this.data.map(d => d.categoryName);
    const dataValues = this.data.map(d => d.amount);
    const colors = this.data.map((d, index) =>
      d.color || this.generateColor(index)
    );

    this.chart.data.labels = labels;
    this.chart.data.datasets[0].data = dataValues;
    this.chart.data.datasets[0].backgroundColor = colors;
    this.chart.update();
  }

  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = undefined;
    }
  }

  private generateColor(index: number): string {
    const colors = [
      'rgba(255, 99, 132, 0.8)',
      'rgba(54, 162, 235, 0.8)',
      'rgba(255, 206, 86, 0.8)',
      'rgba(75, 192, 192, 0.8)',
      'rgba(153, 102, 255, 0.8)',
      'rgba(255, 159, 64, 0.8)',
      'rgba(199, 199, 199, 0.8)',
      'rgba(83, 102, 255, 0.8)',
      'rgba(255, 99, 255, 0.8)',
      'rgba(99, 255, 132, 0.8)',
      'rgba(132, 99, 255, 0.8)',
      'rgba(255, 132, 99, 0.8)'
    ];
    return colors[index % colors.length];
  }
}
