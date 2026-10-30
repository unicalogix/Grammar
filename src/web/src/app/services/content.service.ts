import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

const base = () => `${environment.apiBaseUrl}/api`;

export interface GradeDto {
  id: number;
  nameEn: string;
  nameUr: string | null;
  code: string | null;
  sortOrder: number;
  isActive: boolean;
  ruleIds: number[];
}

/** Flat rule detail (leaf) used by sentences, grades, filters, grid dropdowns. */
export interface RuleDetailDto {
  id: number;
  ruleId: number;
  ruleNameEn: string;
  nameEn: string;
  nameUr: string | null;
  explanationEn: string | null;
  explanationUr: string | null;
  code: string | null;
  isActive: boolean;
  gradeIds: number[];
}

export interface RuleDetailSummaryDto {
  id: number;
  nameEn: string;
  nameUr: string | null;
  code: string | null;
  sortOrder: number;
  isActive: boolean;
}

/** Master rule with nested details. */
export interface RuleDto {
  id: number;
  nameEn: string;
  nameUr: string | null;
  code: string | null;
  sortOrder: number;
  isActive: boolean;
  details: RuleDetailSummaryDto[];
}

export interface SentenceDto {
  id: number;
  textEn: string;
  textUr: string;
  gradeId: number;
  gradeNameEn: string;
  difficulty: number;
  status: number;
  mtSuggestedUr: string | null;
  mtSuggestedEn: string | null;
  mtConfirmedAtUtc: string | null;
  createdAtUtc: string;
  approvedAtUtc: string | null;
  ruleIds: number[];
  ruleCodes: string[];
}

export interface ImportRowError {
  rowNumber: number;
  field: string;
  message: string;
}

export interface ImportBatchDto {
  id: number;
  fileName: string;
  contentType: string | null;
  entityKind: number;
  status: number;
  rowCount: number;
  successCount: number;
  errorCount: number;
  errors: ImportRowError[];
  previewRows: Record<string, string | null>[];
  createdAtUtc: string;
  committedAtUtc: string | null;
}

@Injectable({ providedIn: 'root' })
export class ContentService {
  constructor(private readonly http: HttpClient) {}

  listGrades(activeOnly = false): Observable<GradeDto[]> {
    let params = new HttpParams();
    if (activeOnly) params = params.set('activeOnly', 'true');
    return this.http.get<GradeDto[]>(`${base()}/grades`, { params });
  }

  saveGrade(body: Partial<GradeDto> & { nameEn: string; sortOrder: number; isActive: boolean }, id?: number): Observable<GradeDto> {
    return id
      ? this.http.put<GradeDto>(`${base()}/grades/${id}`, body)
      : this.http.post<GradeDto>(`${base()}/grades`, body);
  }

  setGradeRules(id: number, ruleIds: number[]): Observable<GradeDto> {
    return this.http.put<GradeDto>(`${base()}/grades/${id}/rules`, { ruleIds });
  }

  /** Flat rule details for sentence/grade/filter multi-selects. */
  listRuleDetails(activeOnly = false): Observable<RuleDetailDto[]> {
    let params = new HttpParams();
    if (activeOnly) params = params.set('activeOnly', 'true');
    return this.http.get<RuleDetailDto[]>(`${base()}/rule-details`, { params });
  }

  /** Master rules with nested details (admin page). */
  listRules(activeOnly = false): Observable<RuleDto[]> {
    let params = new HttpParams();
    if (activeOnly) params = params.set('activeOnly', 'true');
    return this.http.get<RuleDto[]>(`${base()}/rules`, { params });
  }

  saveRule(
    body: {
      nameEn: string;
      nameUr?: string | null;
      code?: string | null;
      sortOrder: number;
      isActive: boolean;
      details?: {
        id?: number | null;
        nameEn: string;
        nameUr?: string | null;
        explanationEn?: string | null;
        explanationUr?: string | null;
        code?: string | null;
        sortOrder: number;
        isActive: boolean;
      }[];
    },
    id?: number
  ): Observable<RuleDto> {
    return id
      ? this.http.put<RuleDto>(`${base()}/rules/${id}`, body)
      : this.http.post<RuleDto>(`${base()}/rules`, body);
  }

  deactivateRule(id: number): Observable<void> {
    return this.http.delete<void>(`${base()}/rules/${id}`);
  }

  listSentences(filters: { gradeId?: number; status?: number; difficulty?: number; ruleId?: number } = {}): Observable<SentenceDto[]> {
    let params = new HttpParams();
    if (filters.gradeId != null) params = params.set('gradeId', filters.gradeId);
    if (filters.status != null) params = params.set('status', filters.status);
    if (filters.difficulty != null) params = params.set('difficulty', filters.difficulty);
    if (filters.ruleId != null) params = params.set('ruleId', filters.ruleId);
    return this.http.get<SentenceDto[]>(`${base()}/sentences`, { params });
  }

  saveSentence(body: {
    textEn: string;
    textUr: string;
    gradeId: number;
    difficulty: number;
    status?: number;
    ruleIds?: number[];
  }, id?: number): Observable<SentenceDto> {
    return id
      ? this.http.put<SentenceDto>(`${base()}/sentences/${id}`, body)
      : this.http.post<SentenceDto>(`${base()}/sentences`, body);
  }

  approveSentence(id: number, approve: boolean): Observable<SentenceDto> {
    return this.http.post<SentenceDto>(`${base()}/sentences/${id}/approve`, { approve });
  }

  suggestMt(sentenceId: number): Observable<{ suggestedEn: string | null; suggestedUr: string | null; provider: string; isStub: boolean }> {
    return this.http.post<{ suggestedEn: string | null; suggestedUr: string | null; provider: string; isStub: boolean }>(
      `${base()}/translation/suggest/${sentenceId}`,
      { targetLanguage: 'ur' }
    );
  }

  acceptMt(sentenceId: number): Observable<SentenceDto> {
    return this.http.post<SentenceDto>(`${base()}/sentences/${sentenceId}/accept-mt`, {});
  }

  downloadTemplate(entityKind: string, format: 'csv' | 'xlsx' = 'csv'): Observable<Blob> {
    return this.http.get(`${base()}/import/templates/${entityKind}?format=${format}`, { responseType: 'blob' });
  }

  uploadImport(file: File, entityKind: string): Observable<ImportBatchDto> {
    const form = new FormData();
    form.append('file', file);
    form.append('entityKind', entityKind);
    return this.http.post<ImportBatchDto>(`${base()}/import/batches`, form);
  }

  commitImport(batchId: number): Observable<{ batchId: number; successCount: number; status: number }> {
    return this.http.post<{ batchId: number; successCount: number; status: number }>(
      `${base()}/import/batches/${batchId}/commit`,
      {}
    );
  }
}
