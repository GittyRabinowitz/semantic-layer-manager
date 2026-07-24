import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SemanticApiService } from '../../core/semantic-api.service';
import { NotificationService } from '../../core/notification.service';
import { ConsumableEntity, DataColumn, DataPage } from '../../core/models';

@Component({
  selector: 'app-data-explorer',
  imports: [
    MatCardModule, MatFormFieldModule, MatIconModule, MatPaginatorModule,
    MatProgressBarModule, MatSelectModule, MatTooltipModule
  ],
  templateUrl: './data-explorer.html',
  styleUrl: './data-explorer.scss'
})
export class DataExplorer implements OnInit {
  private readonly api = inject(SemanticApiService);
  private readonly notify = inject(NotificationService);

  readonly entities = signal<ConsumableEntity[]>([]);
  readonly selectedId = signal<number | null>(null);
  readonly data = signal<DataPage | null>(null);
  readonly loading = signal(false);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);

  ngOnInit(): void {
    this.loading.set(true);
    this.api.getConsumableEntities().subscribe({
      next: entities => {
        this.entities.set(entities);
        this.loading.set(false);
        if (entities.length > 0) this.select(entities[0].id);
      },
      error: err => { this.loading.set(false); this.notify.error('Failed to load entities', err); }
    });
  }

  select(id: number): void {
    this.selectedId.set(id);
    this.pageIndex.set(0);
    this.load();
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  /** Presents a raw value in business terms using the column's custom-property hints. */
  formatValue(value: unknown, column: DataColumn): string {
    if (value === null || value === undefined) return '—';
    const format = column.format;

    // Value labels — e.g. status codes to words (N -> New).
    if (format?.valueLabels) {
      const label = format.valueLabels[String(value)];
      if (label !== undefined) return label;
    }

    // Currency — e.g. 349 -> ₪349.00.
    if (format?.currency) {
      const num = Number(value);
      if (!isNaN(num)) {
        const digits = format.decimals ?? 2;
        return new Intl.NumberFormat('he-IL', {
          style: 'currency',
          currency: format.currency,
          minimumFractionDigits: digits,
          maximumFractionDigits: digits
        }).format(num);
      }
    }

    // Dates — honour a custom pattern (e.g. dd/MM/yyyy), else locale default.
    if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
      const date = new Date(value);
      if (!isNaN(date.getTime())) {
        return format?.format ? this.applyDateFormat(date, format.format) : date.toLocaleDateString();
      }
    }

    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    return String(value);
  }

  private applyDateFormat(date: Date, pattern: string): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return pattern
      .replace('yyyy', String(date.getFullYear()))
      .replace('MM', pad(date.getMonth() + 1))
      .replace('dd', pad(date.getDate()));
  }

  private load(): void {
    const id = this.selectedId();
    if (id === null) return;

    this.loading.set(true);
    this.api.getData(id, this.pageIndex() + 1, this.pageSize()).subscribe({
      next: page => { this.data.set(page); this.loading.set(false); },
      error: err => { this.loading.set(false); this.notify.error('Failed to load data', err); }
    });
  }
}
