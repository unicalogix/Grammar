import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  PracticeCatalog,
  PracticeGradeOption,
  PracticeRuleOption,
  PracticeService
} from '../services/practice.service';

@Component({
  standalone: true,
  selector: 'app-practice-setup-page',
  imports: [FormsModule],
  template: `
    <section class="setup">
      <header class="hero">
        <h1>Practice</h1>
        <p>Pick grades, optional rules, difficulty, and which language you see first.</p>
      </header>

      @if (loading()) {
        <p class="muted">Loading catalog…</p>
      } @else {
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

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }

        <button type="button" class="start" (click)="start()" [disabled]="busy() || selectedGrades().size === 0">
          {{ busy() ? 'Starting…' : 'Start practice' }}
        </button>
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
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.practice.catalog().subscribe({
      next: (c: PracticeCatalog) => {
        this.grades.set(c.grades);
        this.rules.set(c.rules);
        if (c.grades.length === 1) {
          this.selectedGrades.set(new Set([c.grades[0].id]));
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.error ?? 'Could not load practice catalog.');
      }
    });
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
