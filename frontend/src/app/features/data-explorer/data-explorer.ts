import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SemanticApiService } from '../../core/semantic-api.service';
import { ConsumableEntity, DataPage } from '../../core/models';

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
  private readonly snack = inject(MatSnackBar);

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
      error: err => { this.loading.set(false); this.fail('Failed to load entities', err); }
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

  formatValue(value: unknown): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
      const date = new Date(value);
      if (!isNaN(date.getTime())) return date.toLocaleDateString();
    }
    return String(value);
  }

  private load(): void {
    const id = this.selectedId();
    if (id === null) return;

    this.loading.set(true);
    this.api.getData(id, this.pageIndex() + 1, this.pageSize()).subscribe({
      next: page => { this.data.set(page); this.loading.set(false); },
      error: err => { this.loading.set(false); this.fail('Failed to load data', err); }
    });
  }

  private fail(message: string, err: unknown): void {
    const detail = err instanceof HttpErrorResponse ? ` (${err.status})` : '';
    console.error(message, err);
    this.snack.open(message + detail, 'Dismiss', { duration: 6000 });
  }
}
