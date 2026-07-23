// TypeScript mirror of the backend DTOs and reports.

export type MappingStatus = 'Mapped' | 'Unmapped' | 'Orphaned' | 'TypeChanged';
export type MetadataSource = 'Introspection' | 'File' | 'User';

export interface SemanticField {
  id: number;
  physicalColumn: string;
  physicalType: string | null;
  displayName: string | null;
  description: string | null;
  isPii: boolean;
  hidden: boolean;
  category: string | null;
  customProperties: unknown | null;
  status: MappingStatus;
  source: MetadataSource;
  lastModified: string;
}

export interface SemanticEntity {
  id: number;
  physicalTable: string;
  displayName: string | null;
  description: string | null;
  fields: SemanticField[];
}

export interface UpdateFieldRequest {
  displayName: string | null;
  description: string | null;
  isPii: boolean;
  hidden: boolean;
  category: string | null;
  customProperties: unknown | null;
}

export type SyncChangeType = 'New' | 'Orphaned' | 'TypeChanged' | 'Restored';

export interface SyncChange {
  table: string;
  column: string;
  changeType: SyncChangeType;
  detail: string | null;
}

export interface SyncReport {
  tablesTotal: number;
  columnsTotal: number;
  newColumns: number;
  orphanedColumns: number;
  typeChangedColumns: number;
  restoredColumns: number;
  mappedColumns: number;
  unmappedColumns: number;
  changes: SyncChange[];
}

export interface MetadataImportReport {
  entitiesInFile: number;
  fieldsInFile: number;
  fieldsApplied: number;
  fieldsUnchanged: number;
  fieldsUnmatched: number;
  unmatchedColumns: string[];
}

// Consumer-side (data explorer).
export interface ConsumableEntity {
  id: number;
  displayName: string;
  physicalTable: string;
}

export interface DataColumn {
  name: string;
  isPii: boolean;
}

export interface DataPage {
  entity: string;
  columns: DataColumn[];
  rows: Record<string, unknown>[];
  page: number;
  pageSize: number;
  totalRows: number;
}
