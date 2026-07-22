import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpErrorResponse } from '@angular/common/http';
import { SemanticApiService } from '../../core/semantic-api.service';
import { MappingStatus, SemanticEntity, SemanticField, SyncReport } from '../../core/models';

@Component({
  selector: 'app-management',
  imports: [
    FormsModule, MatButtonModule, MatCardModule, MatCheckboxModule, MatExpansionModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule,
    MatSlideToggleModule, MatTooltipModule
  ],
  templateUrl: './management.html',
  styleUrl: './management.scss'
})
export class Management implements OnInit {
  private readonly api = inject(SemanticApiService);
  private readonly snack = inject(MatSnackBar);

  readonly entities = signal<SemanticEntity[]>([]);
  readonly loading = signal(false);
  readonly syncReport = signal<SyncReport | null>(null);
  readonly unmappedOnly = signal(false);
  readonly savingId = signal<number | null>(null);

  readonly totalFields = computed(() => this.entities().reduce((n, e) => n + e.fields.length, 0));
  readonly mappedCount = computed(() => this.countStatus('Mapped'));
  readonly unmappedCount = computed(() => this.countStatus('Unmapped'));
  readonly attentionCount = computed(() =>
    this.entities().reduce((n, e) => n + e.fields.filter(f => f.status === 'Orphaned' || f.status === 'TypeChanged').length, 0));

  /** On entry we run a light (idempotent) sync so newly added columns surface immediately. */
  ngOnInit(): void {
    this.loading.set(true);
    this.api.sync().subscribe({
      next: report => { this.syncReport.set(report); this.reload(); },
      error: err => { this.loading.set(false); this.fail('Initial sync failed', err); }
    });
  }

  runSync(): void {
    this.loading.set(true);
    this.api.sync().subscribe({
      next: report => {
        this.syncReport.set(report);
        this.snack.open(
          `Sync complete — ${report.newColumns} new, ${report.orphanedColumns} orphaned, ${report.typeChangedColumns} type-changed`,
          'OK', { duration: 4000 });
        this.reload();
      },
      error: err => { this.loading.set(false); this.fail('Sync failed', err); }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.loading.set(true);
    this.api.importMetadata(file).subscribe({
      next: report => {
        this.snack.open(
          `Imported — ${report.fieldsApplied} applied, ${report.fieldsUnchanged} unchanged, ${report.fieldsUnmatched} unmatched`,
          'OK', { duration: 5000 });
        this.reload();
      },
      error: err => { this.loading.set(false); this.fail('Metadata import failed', err); }
    });
  }

  saveField(field: SemanticField): void {
    this.savingId.set(field.id);
    this.api.updateField(field.id, {
      displayName: field.displayName,
      description: field.description,
      isPii: field.isPii,
      hidden: field.hidden,
      category: field.category,
      customProperties: field.customProperties
    }).subscribe({
      next: updated => {
        Object.assign(field, updated);
        this.savingId.set(null);
        this.snack.open(`Saved "${updated.displayName ?? updated.physicalColumn}"`, 'OK', { duration: 2500 });
      },
      error: err => { this.savingId.set(null); this.fail('Save failed', err); }
    });
  }

  visibleFields(entity: SemanticEntity): SemanticField[] {
    return this.unmappedOnly() ? entity.fields.filter(f => f.status !== 'Mapped') : entity.fields;
  }

  mappedInEntity(entity: SemanticEntity): number {
    return entity.fields.filter(f => f.status === 'Mapped').length;
  }

  statusClass(status: MappingStatus): string {
    return `status status-${status.toLowerCase()}`;
  }

  customPropsText(field: SemanticField): string {
    return field.customProperties ? JSON.stringify(field.customProperties) : '';
  }

  private reload(): void {
    this.loading.set(true);
    this.api.getEntities().subscribe({
      next: entities => { this.entities.set(entities); this.loading.set(false); },
      error: err => { this.loading.set(false); this.fail('Failed to load the semantic model', err); }
    });
  }

  private countStatus(status: MappingStatus): number {
    return this.entities().reduce((n, e) => n + e.fields.filter(f => f.status === status).length, 0);
  }

  private fail(message: string, err: unknown): void {
    const detail = err instanceof HttpErrorResponse ? ` (${err.status})` : '';
    console.error(message, err);
    this.snack.open(message + detail, 'Dismiss', { duration: 6000 });
  }
}
