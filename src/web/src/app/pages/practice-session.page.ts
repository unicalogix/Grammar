import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AttemptResult, PracticeItem, PracticeService, PracticeSession } from '../services/practice.service';

@Component({
  standalone: true,
  selector: 'app-practice-session-page',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="session">
      @if (!session()) {
        <p class="muted">No active session. <a routerLink="/practice">Set up practice</a></p>
      } @else if (finished()) {
        <div class="done">
          <h1>Session complete</h1>
          <p>You finished {{ correctCount() }} of {{ answeredCount() }} correctly.</p>
          <a routerLink="/practice" class="btn">Practice again</a>
        </div>
      } @else if (item()) {
        <div class="progress">
          <span>{{ item()!.sequenceNo }} / {{ session()!.itemCount }}</span>
          <span class="type">{{ exerciseLabel(item()!.exerciseType) }}</span>
        </div>

        <p class="prompt-label">{{ item()!.promptLanguage === 'en' ? 'English' : 'Urdu' }}</p>
        <p
          class="prompt"
          [attr.lang]="item()!.promptLanguage"
          [attr.dir]="item()!.promptLanguage === 'ur' ? 'rtl' : 'ltr'"
        >
          {{ item()!.promptText }}
        </p>

        @if (!feedback()) {
          @if (item()!.exerciseType === 1 && item()!.options) {
            <div class="options" role="listbox">
              @for (opt of item()!.options!; track opt) {
                <button type="button" class="opt" (click)="selectMcq(opt)" [disabled]="busy()">
                  {{ opt }}
                </button>
              }
            </div>
          }

          @if (item()!.exerciseType === 2) {
            <div class="arrange">
              <p class="hint">Tap words in the correct order</p>
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
              <button
                type="button"
                class="submit"
                (click)="submitArrange()"
                [disabled]="busy() || remaining().length > 0"
              >
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
                  [class.ltr]="!isUrduAnswer()"
                  autocomplete="off"
                  spellcheck="false"
                  [disabled]="busy()"
                ></textarea>
              </label>
              @if (isUrduAnswer()) {
                <p class="kb-hint">
                  Answer box is right-to-left for Urdu. Switch to an Urdu keyboard (or OS input method) yourself — the app cannot change your keyboard layout.
                </p>
              }
              <button
                type="button"
                class="submit"
                (click)="submitFree()"
                [disabled]="busy() || !freeAnswer.trim()"
              >
                Check answer
              </button>
            </div>
          }
        } @else {
          <div class="feedback" [class.ok]="feedback()!.isCorrect" [class.bad]="!feedback()!.isCorrect">
            <p class="verdict">{{ feedback()!.isCorrect ? 'Correct' : 'Not quite' }}</p>
            <p class="answer-label">Correct answer</p>
            <p
              class="answer"
              [attr.lang]="answerLang()"
              [attr.dir]="answerDir()"
              [class.rtl]="isUrduAnswer()"
              [class.ltr]="!isUrduAnswer()"
            >
              {{ feedback()!.expectedAnswer }}
            </p>
            <button type="button" class="submit" (click)="continueAfterFeedback()">
              {{ feedback()!.sessionComplete ? 'Finish' : 'Next' }}
            </button>
          </div>
        }

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
      }
    </section>
  `,
  styles: [`
    .session { display: grid; gap: 1rem; max-width: 36rem; margin: 0 auto; }
    .progress {
      display: flex; justify-content: space-between; align-items: center;
      font-weight: 650; color: var(--muted); font-size: .95rem;
    }
    .type {
      padding: .25rem .6rem; border-radius: .4rem;
      background: rgba(31, 107, 74, 0.12); color: var(--accent); font-size: .8rem;
    }
    .prompt-label { margin: 0; font-size: .8rem; font-weight: 650; color: var(--muted); text-transform: uppercase; letter-spacing: .04em; }
    .prompt {
      margin: 0; font-family: var(--font-display);
      font-size: clamp(1.35rem, 3.5vw, 1.85rem); font-weight: 650; line-height: 1.35;
    }
    .options, .pool, .picked { display: flex; flex-wrap: wrap; gap: .55rem; }
    .opt, .token, .submit, .btn {
      font: inherit; font-weight: 650; cursor: pointer;
      min-height: 3rem; padding: .7rem 1rem;
      border-radius: .55rem; border: 1px solid var(--line);
      background: rgba(255,255,255,.8); color: var(--ink); text-align: start;
    }
    .opt { width: 100%; }
    .opt:hover, .token:hover { border-color: var(--accent); }
    .token.on { background: rgba(31, 107, 74, 0.14); border-color: var(--accent); }
    .arrange { display: grid; gap: .85rem; }
    .picked {
      min-height: 3.5rem; padding: .55rem;
      border: 1px dashed var(--line); border-radius: .55rem;
      background: rgba(255,255,255,.4);
    }
    .hint, .muted { color: var(--muted); margin: 0; }
    .freetype { display: grid; gap: .75rem; }
    .freetype label { display: grid; gap: .35rem; font-size: .85rem; color: var(--muted); font-weight: 600; }
    textarea {
      font: inherit; padding: .75rem; border: 1px solid var(--line); border-radius: .55rem;
      background: rgba(255,255,255,.85); resize: vertical; min-height: 5rem;
    }
    textarea.rtl {
      direction: rtl;
      text-align: right;
    }
    textarea.ltr {
      direction: ltr;
      text-align: left;
    }
    .kb-hint {
      margin: 0; font-size: .8rem; color: var(--muted); line-height: 1.4;
    }
    .submit, .btn {
      justify-self: start; border: 0; background: var(--accent); color: #fff;
      text-decoration: none; display: inline-flex; align-items: center;
    }
    .submit:disabled { opacity: .5; cursor: not-allowed; }
    .feedback {
      display: grid; gap: .5rem; padding: 1rem 1.1rem;
      border-radius: .65rem; border: 1px solid var(--line);
      background: rgba(255,255,255,.7);
      animation: rise .35s ease;
    }
    .feedback.ok { border-color: #2a8a5a; background: rgba(42, 138, 90, 0.1); }
    .feedback.bad { border-color: #b54545; background: rgba(181, 69, 69, 0.08); }
    .verdict { margin: 0; font-weight: 700; font-size: 1.1rem; }
    .answer-label { margin: .4rem 0 0; font-size: .8rem; color: var(--muted); font-weight: 650; }
    .answer { margin: 0; font-size: 1.15rem; font-weight: 650; }
    .answer.rtl { direction: rtl; text-align: right; }
    .answer.ltr { direction: ltr; text-align: left; }
    .done { display: grid; gap: .85rem; }
    .done h1 { margin: 0; font-family: var(--font-display); font-size: 2rem; }
    .error { color: #8b1e1e; font-weight: 600; }
    @keyframes rise {
      from { opacity: 0; transform: translateY(6px); }
      to { opacity: 1; transform: none; }
    }
  `]
})
export class PracticeSessionPage implements OnInit {
  readonly session = signal<PracticeSession | null>(null);
  readonly item = signal<PracticeItem | null>(null);
  readonly feedback = signal<AttemptResult | null>(null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly finished = signal(false);
  readonly correctCount = signal(0);
  readonly answeredCount = signal(0);

  readonly arranged = signal<string[]>([]);
  readonly remaining = signal<string[]>([]);
  freeAnswer = '';
  private sessionId = 0;

  constructor(
    private readonly practice: PracticeService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.sessionId = Number(this.route.snapshot.paramMap.get('id'));
    const raw = sessionStorage.getItem('enur_practice_session');
    if (!raw) {
      this.router.navigateByUrl('/practice');
      return;
    }
    try {
      const parsed = JSON.parse(raw) as PracticeSession;
      if (parsed.id !== this.sessionId) {
        this.router.navigateByUrl('/practice');
        return;
      }
      this.session.set(parsed);
      this.loadItem(parsed.item);
    } catch {
      this.router.navigateByUrl('/practice');
    }
  }

  exerciseLabel(type: number): string {
    return type === 1 ? 'Multiple choice' : type === 2 ? 'Arrange words' : 'Type the answer';
  }

  /** Free-type / revealed answer language matches session target (Urdu or English). */
  isUrduAnswer(): boolean {
    return this.session()?.targetLanguage === 'ur';
  }

  answerLang(): 'ur' | 'en' {
    return this.isUrduAnswer() ? 'ur' : 'en';
  }

  answerDir(): 'rtl' | 'ltr' {
    return this.isUrduAnswer() ? 'rtl' : 'ltr';
  }

  selectMcq(opt: string): void {
    this.submit(opt);
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

  submitArrange(): void {
    this.submit(this.arranged().join(' '));
  }

  submitFree(): void {
    this.submit(this.freeAnswer.trim());
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
    const s = this.session();
    if (s) {
      const updated = { ...s, item: fb.nextItem };
      this.session.set(updated);
      sessionStorage.setItem('enur_practice_session', JSON.stringify(updated));
    }
  }

  private loadItem(it: PracticeItem | null | undefined): void {
    this.item.set(it ?? null);
    this.freeAnswer = '';
    this.arranged.set([]);
    this.remaining.set(it?.tokens ? [...it.tokens] : []);
  }

  private submit(answer: string): void {
    const it = this.item();
    if (!it || this.busy()) return;
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
}
