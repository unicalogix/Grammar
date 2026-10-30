import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AttemptResult, PracticeItem, PracticeService, PracticeSession } from '../services/practice.service';
import { SavedTestsService } from '../services/saved-tests.service';

interface GridRowState {
  item: PracticeItem;
  selected: string;
  selectedId: number | null;
  feedback: AttemptResult | null;
  revealed: boolean;
  busy: boolean;
}

@Component({
  standalone: true,
  selector: 'app-test-take-page',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="take">
      @if (loading()) {
        <p class="muted">Starting test…</p>
      } @else if (error() && !session()) {
        <p class="error" role="alert">{{ error() }}</p>
        <a routerLink="/practice" class="btn">Back to Practice</a>
      } @else if (finished()) {
        <div class="done">
          <h1>Test complete</h1>
          @if (session()?.testName) {
            <p class="muted">{{ session()!.testName }}</p>
          }
          <p>You got {{ correctCount() }} of {{ answeredCount() }} correct.</p>
          <a routerLink="/practice" class="btn">Back to Practice</a>
        </div>
      } @else if (isGrid()) {
        <header class="hero">
          <h1>{{ session()?.testName || 'Saved test' }}</h1>
          <p>Answer each question. Correct shows a tick; wrong shows a cross — tap the cross to see the answer.</p>
          <p class="progress">{{ answeredCount() }} / {{ gridRows().length }} answered</p>
        </header>

        <div class="grid">
          @for (row of gridRows(); track row.item.sequenceNo) {
            <article class="q">
              <div class="q-head">
                <span class="num">{{ row.item.sequenceNo }}</span>
                <div class="prompts">
                  @if (row.item.promptEnglish) {
                    <p class="prompt" lang="en" dir="ltr">{{ row.item.promptEnglish }}</p>
                  }
                  @if (row.item.promptTranslation) {
                    <p class="prompt ur" lang="ur" dir="rtl">{{ row.item.promptTranslation }}</p>
                  }
                  @if (!row.item.promptEnglish && !row.item.promptTranslation) {
                    <p
                      class="prompt"
                      [attr.lang]="row.item.promptLanguage"
                      [attr.dir]="row.item.promptLanguage === 'ur' ? 'rtl' : 'ltr'"
                    >
                      {{ row.item.promptText }}
                    </p>
                  }
                </div>
                @if (row.feedback) {
                  @if (row.feedback.isCorrect) {
                    <span class="mark ok" aria-label="Correct">✓</span>
                  } @else {
                    <button
                      type="button"
                      class="mark bad"
                      (click)="row.revealed = true"
                      [attr.aria-label]="row.revealed ? 'Incorrect' : 'Show correct answer'"
                      title="Show correct answer"
                    >
                      ✕
                    </button>
                  }
                }
              </div>

              @if (!row.feedback) {
                <label class="dd">
                  Choose answer
                  <select
                    [(ngModel)]="row.selected"
                    (ngModelChange)="onGridSelect(row, $event)"
                    [disabled]="row.busy"
                  >
                    <option [ngValue]="''">— select —</option>
                    @if (row.item.optionIds && row.item.options) {
                      @for (opt of row.item.options; track $index) {
                        <option [ngValue]="optionValue(row, $index)">{{ opt }}</option>
                      }
                    } @else if (row.item.options) {
                      @for (opt of row.item.options; track opt) {
                        <option [ngValue]="opt">{{ opt }}</option>
                      }
                    }
                  </select>
                </label>
                <button
                  type="button"
                  class="submit"
                  (click)="submitGrid(row)"
                  [disabled]="row.busy || !row.selected"
                >
                  Check
                </button>
              } @else if (row.revealed || row.feedback.isCorrect) {
                <p class="answer-label">Correct answer</p>
                <p class="answer">{{ row.feedback.expectedAnswer }}</p>
              } @else {
                <p class="hint">Tap ✕ to reveal the correct answer</p>
              }
            </article>
          }
        </div>

        @if (allGridAnswered()) {
          <button type="button" class="submit finish" (click)="finishGrid()">Finish</button>
        }
      } @else if (item()) {
        <!-- Sequential: same interaction as free practice -->
        <header class="hero compact">
          @if (session()?.testName) {
            <p class="muted">{{ session()!.testName }}</p>
          }
          <div class="progress-bar">
            <span>{{ item()!.sequenceNo }} / {{ session()!.itemCount }}</span>
            <span class="type">{{ exerciseLabel(item()!.exerciseType) }}</span>
          </div>
        </header>

        <div class="prompts sequential">
          @if (item()!.promptEnglish) {
            <p class="prompt-label">English</p>
            <p class="prompt big" lang="en" dir="ltr">{{ item()!.promptEnglish }}</p>
          }
          @if (item()!.promptTranslation) {
            <p class="prompt-label">Translation</p>
            <p class="prompt big ur" lang="ur" dir="rtl">{{ item()!.promptTranslation }}</p>
          }
          @if (!item()!.promptEnglish && !item()!.promptTranslation) {
            <p class="prompt-label">{{ item()!.promptLanguage === 'en' ? 'English' : 'Urdu' }}</p>
            <p
              class="prompt big"
              [attr.lang]="item()!.promptLanguage"
              [attr.dir]="item()!.promptLanguage === 'ur' ? 'rtl' : 'ltr'"
            >
              {{ item()!.promptText }}
            </p>
          }
        </div>

        @if (!feedback()) {
          @if (item()!.exerciseType === 1 && item()!.options) {
            <div class="options">
              @for (opt of item()!.options!; track opt) {
                <button type="button" class="opt" (click)="submitSeq(opt)" [disabled]="busy()">{{ opt }}</button>
              }
            </div>
          }
          @if (item()!.exerciseType === 2 || item()!.exerciseType === 5) {
            <div class="arrange">
              <p class="hint">
                @if (item()!.exerciseType === 5) {
                  Tap words in order — leave out extras
                } @else {
                  Tap words in the correct order
                }
              </p>
              <div class="picked">
                @for (w of arranged(); track $index) {
                  <button type="button" class="token on" (click)="unpick($index)" [disabled]="busy()">{{ w }}</button>
                }
              </div>
              <div class="pool">
                @for (w of remaining(); track $index) {
                  <button type="button" class="token" (click)="pick(w, $index)" [disabled]="busy()">{{ w }}</button>
                }
              </div>
              <button type="button" class="submit" (click)="submitSeq(arranged().join(' '))" [disabled]="busy() || !canSubmitArrange()">
                Check answer
              </button>
            </div>
          }
          @if (item()!.exerciseType === 3) {
            <div class="freetype">
              <label>
                Your answer
                <textarea
                  rows="3"
                  [(ngModel)]="freeAnswer"
                  [attr.lang]="answerLang()"
                  [attr.dir]="answerDir()"
                  [class.rtl]="isUrduAnswer()"
                  [disabled]="busy()"
                ></textarea>
              </label>
              <button type="button" class="submit" (click)="submitSeq(freeAnswer.trim())" [disabled]="busy() || !freeAnswer.trim()">
                Check answer
              </button>
            </div>
          }
          @if (item()!.exerciseType === 4) {
            <div class="fillblank">
              @if (item()!.blankedText) {
                <p class="blanked" [attr.dir]="answerDir()">{{ item()!.blankedText }}</p>
              }
              <p class="hint">Choose the missing word</p>
              @if (item()!.options) {
                <div class="options">
                  @for (opt of item()!.options!; track opt) {
                    <button type="button" class="opt" (click)="submitSeq(opt)" [disabled]="busy()">{{ opt }}</button>
                  }
                </div>
              }
            </div>
          }
          @if (item()!.exerciseType === 6 && item()!.options && item()!.optionIds) {
            <div class="rules">
              <p class="hint">Which rules apply? Select all that fit.</p>
              <div class="rule-list">
                @for (opt of item()!.options!; track $index) {
                  <label class="rule-opt">
                    <input
                      type="checkbox"
                      [checked]="selectedRuleIds().has(item()!.optionIds![$index])"
                      (change)="toggleRule(item()!.optionIds![$index])"
                      [disabled]="busy()"
                    />
                    <span>{{ opt }}</span>
                  </label>
                }
              </div>
              <button
                type="button"
                class="submit"
                (click)="submitRules()"
                [disabled]="busy() || selectedRuleIds().size === 0"
              >
                Check answer
              </button>
            </div>
          }
        } @else {
          <div class="feedback" [class.ok]="feedback()!.isCorrect" [class.bad]="!feedback()!.isCorrect">
            <p class="verdict">{{ feedback()!.isCorrect ? 'Correct' : 'Not quite' }}</p>
            <p class="answer-label">Correct answer</p>
            <p class="answer">{{ feedback()!.expectedAnswer }}</p>
            <button type="button" class="submit" (click)="continueAfterFeedback()">
              {{ feedback()!.sessionComplete ? 'Finish' : 'Next' }}
            </button>
          </div>
        }
      }

      @if (error() && session()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
  styles: [`
    .take { display: grid; gap: 1rem; max-width: 42rem; margin: 0 auto; }
    .hero h1 {
      margin: 0; font-family: var(--font-display);
      font-size: clamp(1.5rem, 3.5vw, 2rem); font-weight: 650;
    }
    .hero p { margin: .35rem 0 0; color: var(--muted); }
    .hero.compact { display: grid; gap: .35rem; }
    .progress, .progress-bar {
      display: flex; justify-content: space-between; align-items: center;
      font-weight: 650; color: var(--muted); font-size: .95rem;
    }
    .type {
      padding: .25rem .6rem; border-radius: .4rem;
      background: rgba(31, 107, 74, 0.12); color: var(--accent); font-size: .8rem;
    }
    .grid { display: grid; gap: .85rem; }
    .q {
      display: grid; gap: .65rem; padding: .9rem 1rem;
      border: 1px solid var(--line); border-radius: .55rem;
      background: rgba(255,255,255,.6);
    }
    .q-head { display: grid; grid-template-columns: auto 1fr auto; gap: .65rem; align-items: start; }
    .num {
      width: 1.75rem; height: 1.75rem; border-radius: .4rem;
      display: grid; place-items: center;
      background: rgba(31, 107, 74, 0.12); color: var(--accent);
      font-weight: 700; font-size: .85rem;
    }
    .prompt { margin: 0; font-weight: 650; line-height: 1.35; }
    .prompt.big {
      font-family: var(--font-display);
      font-size: clamp(1.35rem, 3.5vw, 1.85rem);
    }
    .prompt-label {
      margin: 0; font-size: .8rem; font-weight: 650; color: var(--muted);
      text-transform: uppercase; letter-spacing: .04em;
    }
    .mark {
      width: 2.25rem; height: 2.25rem; border-radius: .45rem;
      display: grid; place-items: center; font-size: 1.15rem; font-weight: 700;
      border: 1px solid transparent;
    }
    .mark.ok { background: rgba(42, 138, 90, 0.15); color: #1f6b4a; }
    .mark.bad {
      background: rgba(181, 69, 69, 0.12); color: #8b1e1e;
      cursor: pointer; font: inherit; font-size: 1.15rem; font-weight: 700;
    }
    .dd { display: grid; gap: .3rem; font-size: .85rem; color: var(--muted); font-weight: 600; }
    select, textarea {
      font: inherit; min-height: 2.75rem; padding: .55rem .7rem;
      border: 1px solid var(--line); border-radius: .45rem;
      background: rgba(255,255,255,.85); color: var(--ink);
    }
    textarea.rtl { direction: rtl; text-align: right; }
    .submit, .btn, .opt, .token {
      font: inherit; font-weight: 650; cursor: pointer;
      min-height: 2.75rem; padding: .65rem 1rem;
      border-radius: .55rem; border: 1px solid var(--line);
      background: rgba(255,255,255,.8); color: var(--ink); text-align: start;
    }
    .submit, .btn {
      border: 0; background: var(--accent); color: #fff;
      justify-self: start; text-decoration: none; display: inline-flex; align-items: center;
    }
    .submit:disabled { opacity: .5; cursor: not-allowed; }
    .submit.finish { margin-top: .5rem; }
    .options, .pool, .picked { display: flex; flex-wrap: wrap; gap: .55rem; }
    .opt { width: 100%; }
    .token.on { background: rgba(31, 107, 74, 0.14); border-color: var(--accent); }
    .picked {
      min-height: 3.25rem; padding: .55rem;
      border: 1px dashed var(--line); border-radius: .55rem;
    }
    .arrange, .fillblank, .rules, .freetype { display: grid; gap: .75rem; }
    .blanked {
      margin: 0; font-size: 1.2rem; font-weight: 650;
      padding: .75rem 1rem; border-radius: .55rem;
      background: rgba(255,255,255,.55); border: 1px solid var(--line);
    }
    .rule-list { display: grid; gap: .55rem; }
    .rule-opt {
      display: flex; gap: .65rem; align-items: flex-start;
      min-height: 2.75rem; padding: .65rem .85rem;
      border: 1px solid var(--line); border-radius: .55rem;
      background: rgba(255,255,255,.8); cursor: pointer; font-weight: 600;
    }
    .feedback {
      display: grid; gap: .5rem; padding: 1rem 1.1rem;
      border-radius: .65rem; border: 1px solid var(--line);
      background: rgba(255,255,255,.7);
    }
    .feedback.ok { border-color: #2a8a5a; background: rgba(42, 138, 90, 0.1); }
    .feedback.bad { border-color: #b54545; background: rgba(181, 69, 69, 0.08); }
    .verdict { margin: 0; font-weight: 700; font-size: 1.1rem; }
    .answer-label { margin: 0; font-size: .8rem; color: var(--muted); font-weight: 650; }
    .answer { margin: 0; font-size: 1.1rem; font-weight: 650; }
    .hint, .muted { color: var(--muted); margin: 0; }
    .done { display: grid; gap: .85rem; }
    .done h1 { margin: 0; font-family: var(--font-display); font-size: 2rem; }
    .error { color: #8b1e1e; font-weight: 600; }
  `]
})
export class TestTakePage implements OnInit {
  readonly loading = signal(true);
  readonly session = signal<PracticeSession | null>(null);
  readonly item = signal<PracticeItem | null>(null);
  readonly feedback = signal<AttemptResult | null>(null);
  readonly gridRows = signal<GridRowState[]>([]);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly finished = signal(false);
  readonly correctCount = signal(0);
  readonly answeredCount = signal(0);
  readonly arranged = signal<string[]>([]);
  readonly remaining = signal<string[]>([]);
  readonly selectedRuleIds = signal<Set<number>>(new Set());
  freeAnswer = '';
  private sessionId = 0;

  constructor(
    private readonly savedTests: SavedTestsService,
    private readonly practice: PracticeService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.router.navigateByUrl('/practice');
      return;
    }
    this.savedTests.start(id).subscribe({
      next: (session) => {
        this.loading.set(false);
        if (session.emptyMessage || session.id === 0) {
          this.error.set(session.emptyMessage ?? 'No matching sentences for this test.');
          return;
        }
        this.session.set(session);
        this.sessionId = session.id;
        sessionStorage.setItem('enur_practice_session', JSON.stringify(session));
        if (session.layout === 2 && session.items?.length) {
          this.gridRows.set(
            session.items.map((it) => ({
              item: it,
              selected: '',
              selectedId: null,
              feedback: null,
              revealed: false,
              busy: false
            }))
          );
        } else {
          this.loadItem(session.item);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.error ?? 'Could not start saved test.');
      }
    });
  }

  isGrid(): boolean {
    return this.session()?.layout === 2;
  }

  allGridAnswered(): boolean {
    const rows = this.gridRows();
    return rows.length > 0 && rows.every((r) => r.feedback);
  }

  optionValue(row: GridRowState, index: number): string {
    return String(row.item.optionIds![index]);
  }

  onGridSelect(row: GridRowState, value: string): void {
    row.selected = value ?? '';
    if (row.item.optionIds) {
      const n = Number(value);
      row.selectedId = Number.isFinite(n) && value !== '' ? n : null;
    } else {
      row.selectedId = null;
    }
  }

  submitGrid(row: GridRowState): void {
    if (!row.selected || row.busy || row.feedback) return;
    row.busy = true;
    this.error.set(null);
    const answer =
      row.item.optionIds && row.selectedId != null ? String(row.selectedId) : row.selected;
    this.practice
      .submitAttempt(this.sessionId, {
        sequenceNo: row.item.sequenceNo,
        sentenceId: row.item.sentenceId,
        exerciseType: row.item.exerciseType,
        studentAnswer: answer
      })
      .subscribe({
        next: (res) => {
          row.busy = false;
          row.feedback = res;
          if (res.isCorrect) row.revealed = true;
          this.answeredCount.update((n) => n + 1);
          if (res.isCorrect) this.correctCount.update((n) => n + 1);
          // trigger signal refresh
          this.gridRows.set([...this.gridRows()]);
          if (res.sessionComplete) {
            // keep grid visible until Finish
          }
        },
        error: (err) => {
          row.busy = false;
          this.gridRows.set([...this.gridRows()]);
          this.error.set(err?.error?.error ?? 'Could not submit answer.');
        }
      });
  }

  finishGrid(): void {
    this.finished.set(true);
    sessionStorage.removeItem('enur_practice_session');
    this.practice.complete(this.sessionId).subscribe({ error: () => undefined });
  }

  exerciseLabel(type: number): string {
    switch (type) {
      case 1: return 'Multiple choice';
      case 2: return 'Arrange words';
      case 3: return 'Type the answer';
      case 4: return 'Fill in the blank';
      case 5: return 'Arrange (extra words)';
      case 6: return 'Which rules apply?';
      case 7: return 'Dropdown';
      default: return 'Exercise';
    }
  }

  isUrduAnswer(): boolean {
    return this.session()?.targetLanguage === 'ur';
  }

  answerLang(): 'ur' | 'en' {
    return this.isUrduAnswer() ? 'ur' : 'en';
  }

  answerDir(): 'rtl' | 'ltr' {
    return this.isUrduAnswer() ? 'rtl' : 'ltr';
  }

  pick(word: string, index: number): void {
    const pool = [...this.remaining()];
    pool.splice(index, 1);
    this.remaining.set(pool);
    this.arranged.set([...this.arranged(), word]);
  }

  unpick(index: number): void {
    const picked = [...this.arranged()];
    const [word] = picked.splice(index, 1);
    this.arranged.set(picked);
    this.remaining.set([...this.remaining(), word]);
  }

  canSubmitArrange(): boolean {
    const it = this.item();
    if (!it || this.arranged().length === 0) return false;
    if (it.exerciseType === 5) return true;
    return this.remaining().length === 0;
  }

  toggleRule(id: number): void {
    const next = new Set(this.selectedRuleIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedRuleIds.set(next);
  }

  submitRules(): void {
    const ids = [...this.selectedRuleIds()].sort((a, b) => a - b);
    this.submitSeq(ids.join(','));
  }

  submitSeq(answer: string): void {
    const it = this.item();
    if (!it || this.busy() || !answer) return;
    this.busy.set(true);
    this.error.set(null);
    this.practice
      .submitAttempt(this.sessionId, {
        sequenceNo: it.sequenceNo,
        sentenceId: it.sentenceId,
        exerciseType: it.exerciseType,
        studentAnswer: answer
      })
      .subscribe({
        next: (res) => {
          this.busy.set(false);
          this.answeredCount.update((n) => n + 1);
          if (res.isCorrect) this.correctCount.update((n) => n + 1);
          this.feedback.set(res);
        },
        error: (err) => {
          this.busy.set(false);
          this.error.set(err?.error?.error ?? 'Could not submit answer.');
        }
      });
  }

  continueAfterFeedback(): void {
    const fb = this.feedback();
    if (!fb) return;
    if (fb.sessionComplete || !fb.nextItem) {
      this.finished.set(true);
      this.feedback.set(null);
      this.item.set(null);
      sessionStorage.removeItem('enur_practice_session');
      this.practice.complete(this.sessionId).subscribe({ error: () => undefined });
      return;
    }
    this.feedback.set(null);
    this.loadItem(fb.nextItem);
  }

  private loadItem(it: PracticeItem | null | undefined): void {
    this.item.set(it ?? null);
    this.freeAnswer = '';
    this.arranged.set([]);
    this.remaining.set(it?.tokens ? [...it.tokens] : []);
    this.selectedRuleIds.set(new Set());
  }
}
