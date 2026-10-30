import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PracticeSession } from './practice.service';

const base = () => `${environment.apiBaseUrl}/api/saved-tests`;

export interface SavedTestSummary {
  id: number;
  name: string;
  description: string | null;
  layout: number;
  dropdownMode: number;
  exerciseTypes: number[];
  itemCount: number;
  isActive: boolean;
}

export interface SavedTest extends SavedTestSummary {
  gradeIds: number[];
  ruleIds: number[];
  difficulty: number | null;
  sourceLanguage: string;
  showEnglish: boolean;
  showTranslation: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface UpsertSavedTestRequest {
  name: string;
  description?: string | null;
  layout: number;
  dropdownMode: number;
  exerciseTypes: number[];
  gradeIds: number[];
  ruleIds?: number[];
  difficulty?: number | null;
  sourceLanguage?: string | null;
  showEnglish: boolean;
  showTranslation: boolean;
  itemCount: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SavedTestsService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<SavedTestSummary[]> {
    return this.http.get<SavedTestSummary[]>(base());
  }

  get(id: number): Observable<SavedTest> {
    return this.http.get<SavedTest>(`${base()}/${id}`);
  }

  create(body: UpsertSavedTestRequest): Observable<SavedTest> {
    return this.http.post<SavedTest>(base(), body);
  }

  update(id: number, body: UpsertSavedTestRequest): Observable<SavedTest> {
    return this.http.put<SavedTest>(`${base()}/${id}`, body);
  }

  deactivate(id: number): Observable<void> {
    return this.http.delete<void>(`${base()}/${id}`);
  }

  start(id: number): Observable<PracticeSession> {
    return this.http.post<PracticeSession>(`${base()}/${id}/start`, {});
  }
}
