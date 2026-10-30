import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

const base = () => `${environment.apiBaseUrl}/api/practice`;

export interface PracticeGradeOption {
  id: number;
  nameEn: string;
  nameUr: string | null;
  code: string | null;
  sortOrder: number;
}

export interface PracticeRuleOption {
  id: number;
  nameEn: string;
  nameUr: string | null;
  code: string | null;
  ruleId: number;
  ruleNameEn: string;
  gradeIds: number[];
}

export interface PracticeCatalog {
  grades: PracticeGradeOption[];
  rules: PracticeRuleOption[];
}

/** 1 MCQ, 2 Arrange, 3 FreeType, 4 FillBlank, 5 ArrangeDistractors, 6 RulesApply */
export interface PracticeItem {
  sequenceNo: number;
  sentenceId: number;
  exerciseType: number;
  promptLanguage: string;
  promptText: string;
  options: string[] | null;
  tokens: string[] | null;
  blankedText: string | null;
  optionIds: number[] | null;
  promptEnglish?: string | null;
  promptTranslation?: string | null;
  showEnglish?: boolean;
  showTranslation?: boolean;
}

export interface PracticeSession {
  id: number;
  sourceLanguage: string;
  targetLanguage: string;
  itemCount: number;
  startedAtUtc: string;
  endedAtUtc: string | null;
  emptyMessage: string | null;
  item: PracticeItem | null;
  /** All items for Grid layout. */
  items?: PracticeItem[] | null;
  savedTestId?: number | null;
  layout?: number | null;
  testName?: string | null;
}

export interface AttemptResult {
  attemptId: number;
  sequenceNo: number;
  isCorrect: boolean;
  expectedAnswer: string;
  studentAnswer: string;
  nextItem: PracticeItem | null;
  sessionComplete: boolean;
  correctOptionIds?: number[] | null;
}

export interface StartPracticeRequest {
  gradeIds: number[];
  ruleIds?: number[];
  difficulty?: number | null;
  sourceLanguage: string;
  itemCount?: number;
}

@Injectable({ providedIn: 'root' })
export class PracticeService {
  constructor(private readonly http: HttpClient) {}

  catalog(): Observable<PracticeCatalog> {
    return this.http.get<PracticeCatalog>(`${base()}/catalog`);
  }

  start(body: StartPracticeRequest): Observable<PracticeSession> {
    return this.http.post<PracticeSession>(`${base()}/sessions`, body);
  }

  submitAttempt(
    sessionId: number,
    body: {
      sequenceNo: number;
      sentenceId: number;
      exerciseType: number;
      studentAnswer: string;
    }
  ): Observable<AttemptResult> {
    return this.http.post<AttemptResult>(`${base()}/sessions/${sessionId}/attempts`, body);
  }

  complete(sessionId: number): Observable<PracticeSession> {
    return this.http.post<PracticeSession>(`${base()}/sessions/${sessionId}/complete`, {});
  }
}
