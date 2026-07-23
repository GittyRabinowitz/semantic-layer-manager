import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ConsumableEntity, DataPage, MetadataImportReport, SemanticEntity, SemanticField, SyncReport, UpdateFieldRequest
} from './models';

/** Typed client for the Semantic Layer Manager backend API. */
@Injectable({ providedIn: 'root' })
export class SemanticApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  // ── Management ──
  getEntities(): Observable<SemanticEntity[]> {
    return this.http.get<SemanticEntity[]>(`${this.base}/semantic/entities`);
  }

  updateField(id: number, request: UpdateFieldRequest): Observable<SemanticField> {
    return this.http.put<SemanticField>(`${this.base}/semantic/fields/${id}`, request);
  }

  // ── Sync ──
  sync(): Observable<SyncReport> {
    return this.http.post<SyncReport>(`${this.base}/sync`, {});
  }

  // ── Metadata import ──
  importMetadata(file: File): Observable<MetadataImportReport> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<MetadataImportReport>(`${this.base}/metadata/import`, form);
  }

  // ── Consumer data ──
  getConsumableEntities(): Observable<ConsumableEntity[]> {
    return this.http.get<ConsumableEntity[]>(`${this.base}/data/entities`);
  }

  getData(entityId: number, page: number, pageSize: number): Observable<DataPage> {
    return this.http.get<DataPage>(`${this.base}/data/${entityId}`, { params: { page, pageSize } });
  }
}
