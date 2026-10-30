import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  PracticeCatalog,
  PracticeGradeOption,
  PracticeRuleOption,
  PracticeService
} from '../services/practice.service';
import { SavedTestSummary, SavedTestsService } from '../services/saved-tests.service';

@Component({
  standalone: true,
  selector: 'app-practice-setup-page',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="setup">
      <header class="hero">
        <h1>Practice</h1>
        <p>Take a saved test from your teacher, or start a free practice session.</p>
      </header>

      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else {
        @if (savedTests().length > 0) {
          <section class="saved">
            <h2>Saved tests</h2>
            <ul class="test-list">
              @for (t of savedTests(); track t.id) {
                <li>
                  <div class="meta">
                    <strong>{{ t.name }}</strong>
                    <span class="tag">{{ layoutLabel(t) }} · {{ t.itemCount }} items</span>
                    @if (t.description) {
                      <p class="desc">{{ t.description }}</p>
                    }
                  </div>
                  <a class="start-link" [routerLink]="['/tests', t.id, 'take']">Take test</a>
                </li>
              }
            </ul>
          </section>
        }

        <details class="free" [open]="savedTests().length === 0">
          <summary>Free practice setup</summary>
          <div class="free-body">
            <fieldset class="block">
              <legend>Grades</legend>
              <div class="chips">
                @for (g of grades(); track g.id) {
                  <label class="chip">
                    <input type="checkbox" [checked]="selectedGrades().has(g.id)" (change)="toggleGrade(g.id)" />
                    <span>{{ g.nameEn }}@if (g.nameUr) { · {{ g.nameUr }}}</span>
                  </label>
                }
              </div>
            </fieldset>

            <fieldset class="block">
              <legend>Rules / tenses <span class="opt">(optional)</span></legend>
              <div class="chips">
                @for (r of filteredRules(); track r.id) {
                  <label class="chip">
                    <input type="checkbox" [checked]="selectedRules().has(r.id)" (change)="toggleRule(r.id)" />
                    <span>{{ r.nameEn }}@if (r.code) { <code>{{ r.code }}</code> }</span>
                  </label>
                }
                @if (filteredRules().length === 0) {
                  <p class="muted">Select a grade to see linked rules, or leave empty for all.</p>
                }
              </div>
            </fieldset>

            <fieldset class="block row">
              <label>
                Difficulty
                <select [(ngModel)]="difficulty">
                  <option [ngValue]="null">Any</option>
                  <option [ngValue]="1">Easy</option>
                  <option [ngValue]="2">Medium</option>
                  <option [ngValue]="3">Hard</option>
                </select>
              </label>
              <label>
                Prompt language
                <select [(ngModel)]="sourceLanguage">
                  <option value="en">English → translate to Urdu</option>
                  <option value="ur">Urdu → translate to English</option>
                </select>
              </label>
              <label>
                Items
                <select [(ngModel)]="itemCount">
                  <option [ngValue]="5">5</option>
                  <option [ngValue]="8">8</option>
                  <option [ngValue]="12">12</option>
                </select>
              </label>
            </fieldset>

            <button type="button" class="start" (click)="start()" [disabled]="busy() || selectedGrades().size === 0">
              {{ busy() ? 'Starting…' : 'Start practice' }}
            </button>
          </div>
        </details>

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
      }
    </section>
  `,
  styles: [`
    .setup { display: grid; gap: 1.25rem; max-width: 40rem; }
    .hero h1 {
      margin: 0;
      font-family: var(--font-display);
      font-size: clamp(1.75rem, 4vw, 2.35rem);
      font-weight: 650;
      letter-spacing: -0.02em;
    }
    .hero p { margin: .4rem 0 0; color: var(--muted); }
    .saved h2 { margin: 0 0 .75rem; font-size: 1.1rem; }
    .test-list { list-style: none; margin: 0; padding: 0; display: grid; gap: .65rem; }
    .test-list li {
      display: flex; flex-wrap: wrap; gap: .75rem; align-items: center; justify-content: space-between;
      padding: .85rem 1rem; border: 1px solid var(--line); border-radius: .55rem;
      background: rgba(255,255,255,.55);
    }
    .meta { display: grid; gap: .2rem; flex: 1; min-width: 12rem; }
    .tag { font-size: .8rem; color: var(--muted); font-weight: 600; }
    .desc { margin: 0; font-size: .9rem; color: var(--muted); }
    .start-link {
      font: inherit; font-weight: 700; text-decoration: none;
      min-height: 2.75rem; padding: .55rem 1rem; border-radius: .45rem;
      background: var(--accent); color: #fff; display: inline-flex; align-items: center;
    }
    .free {
      border: 1px solid var(--line); border-radius: .65rem;
      background: rgba(255,255,255,.45); padding: .35rem .85rem 1rem;
    }
    .free summary {
      cursor: pointer; font-weight: 700; padding: .65rem 0; list-style-position: outside;
    }
    .free-body { display: grid; gap: 1rem; padding-top: .25rem; }
    .block {
      margin: 0; padding: 1rem 1.1rem;
      border: 1px solid var(--line);
      border-radius: .65rem;
      background: rgba(255,255,255,.55);
    }
    legend { font-weight: 650; padding: 0 .25rem; }
    .opt { font-weight: 400; color: var(--muted); font-size: .85rem; }
    .chips { display: flex; flex-wrap: wrap; gap: .55rem; margin-top: .65rem; }
    .chip {
      display: inline-flex; align-items: center; gap: .45rem;
      min-height: 2.75rem; padding: .45rem .85rem;
      border: 1px solid var(--line); border-radius: .55rem;
      background: rgba(255,255,255,.75); cursor: pointer; font-weight: 600;
    }
    .chip:has(input:checked) {
      border-color: var(--accent);
      background: rgba(31, 107, 74, 0.12);
      color: var(--accent);
    }
    .chip input { width: 1.15rem; height: 1.15rem; accent-color: var(--accent); }
    .chip code { font-size: .75rem; font-weight: 600; opacity: .75; }
    .row {
      display: grid; gap: .85rem;
      grid-template-columns: 1fr;
    }
    @media (min-width: 640px) {
      .row { grid-template-columns: repeat(3, 1fr); }
    }
    label { display: grid; gap: .35rem; font-size: .85rem; color: var(--muted); font-weight: 600; }
    select {
      font: inherit; min-height: 2.75rem; padding: .55rem .7rem;
      border: 1px solid var(--line); border-radius: .45rem;
      background: rgba(255,255,255,.85); color: var(--ink);
    }
    .start {
      font: inherit; font-weight: 700; font-size: 1.05rem;
      min-height: 3rem; padding: .75rem 1.25rem;
      border: 0; border-radius: .55rem; cursor: pointer;
      color: #fff; background: var(--accent);
      justify-self: start;
    }
    .start:disabled { opacity: .55; cursor: not-allowed; }
    .muted { color: var(--muted); margin: 0; }
    .error { color: #8b1e1e; margin: 0; font-weight: 600; }
  `]
})
export class PracticeSetupPage implements OnInit {
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly grades = signal<PracticeGradeOption[]>([]);
  readonly rules = signal<PracticeRuleOption[]>([]);
  readonly savedTests = signal<SavedTestSummary[]>([]);
  readonly selectedGrades = signal(new Set<number>());
  readonly selectedRules = signal(new Set<number>());

  difficulty: number | null = null;
  sourceLanguage = 'en';
  itemCount = 8;

  readonly filteredRules = computed(() => {
    const selected = this.selectedGrades();
    const all = this.rules();
    if (selected.size === 0) return all;
    return all.filter((r) => r.gradeIds.some((id) => selected.has(id)) || r.gradeIds.length === 0);
  });

  constructor(
    private readonly practice: PracticeService,
    private readonly savedTestsApi: SavedTestsService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    let pending = 2;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.loading.set(false);
    };

    this.practice.catalog().subscribe({
      next: (c: PracticeCatalog) => {
        this.grades.set(c.grades);
        this.rules.set(c.rules);
        if (c.grades.length === 1) {
          this.selectedGrades.set(new Set([c.grades[0].id]));
        }
        done();
      },
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Could not load practice catalog.');
        done();
      }
    });

    this.savedTestsApi.list().subscribe({
      next: (rows) => {
        this.savedTests.set(rows.filter((t) => t.isActive));
        done();
      },
      error: () => done()
    });
  }

  layoutLabel(t: SavedTestSummary): string {
    if (t.layout === 2) {
      return t.dropdownMode === 1 ? 'Grid · rules' : 'Grid · translation';
    }
    return 'Sequential';
  }

  toggleGrade(id: number): void {
    const next = new Set(this.selectedGrades());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedGrades.set(next);
  }

  toggleRule(id: number): void {
    const next = new Set(this.selectedRules());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedRules.set(next);
  }

  start(): void {
    if (this.selectedGrades().size === 0) {
      this.error.set('Select at least one grade.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    const ruleIds = [...this.selectedRules()];
    this.practice
      .start({
        gradeIds: [...this.selectedGrades()],
        ruleIds: ruleIds.length ? ruleIds : undefined,
        difficulty: this.difficulty,
        sourceLanguage: this.sourceLanguage,
        itemCount: this.itemCount
      })
      .subscribe({
        next: (session) => {
          this.busy.set(false);
          if (session.emptyMessage || !session.item || session.id === 0) {
            this.error.set(session.emptyMessage ?? 'No matching sentences.');
            return;
          }
          sessionStorage.setItem('enur_practice_session', JSON.stringify(session));
          this.router.navigate(['/practice', session.id]);
        },
        error: (err) => {
          this.busy.set(false);
          this.error.set(err?.error?.error ?? 'Could not start practice.');
        }
      });
  }
}
