import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService, GradeDto, RuleDto } from '../services/content.service';
import {
  SavedTest,
  SavedTestSummary,
  SavedTestsService,
  UpsertSavedTestRequest
} from '../services/saved-tests.service';

const EXERCISE_OPTIONS: { value: number; label: string }[] = [
  { value: 1, label: 'Multiple choice' },
  { value: 2, label: 'Arrange words' },
  { value: 3, label: 'Free type' },
  { value: 4, label: 'Fill in the blank' },
  { value: 5, label: 'Arrange + distractors' },
  { value: 6, label: 'Which rules apply?' }
];

@Component({
  standalone: true,
  selector: 'app-admin-tests-page',
  imports: [FormsModule],
  template: `
    <div class="head">
      <h2>Saved tests</h2>
      <button type="button" (click)="startNew()">New test</button>
    </div>
    <p class="lede">
      Define reusable tests: sequential (one question at a time) or grid (all questions with dropdowns).
      Students take them from Practice.
    </p>

    @if (editing()) {
      <section class="form wide">
        <h3>{{ form.id ? 'Edit' : 'New' }} test</h3>
        <label>Name<input [(ngModel)]="form.name" required placeholder="Grade 5 past simple quiz" /></label>
        <label>Description<textarea rows="2" [(ngModel)]="form.description" placeholder="Optional"></textarea></label>

        <div class="row2">
          <label>
            Layout
            <select [(ngModel)]="form.layout" (ngModelChange)="onLayoutChange()">
              <option [ngValue]="1">Sequential (one at a time)</option>
              <option [ngValue]="2">Grid (all on one page)</option>
            </select>
          </label>
          @if (form.layout === 2) {
            <label>
              Dropdown options
              <select [(ngModel)]="form.dropdownMode">
                <option [ngValue]="1">Rule / tense names</option>
                <option [ngValue]="2">Translations</option>
              </select>
            </label>
          }
        </div>

        @if (form.layout === 1) {
          <fieldset class="types">
            <legend>Exercise types</legend>
            <div class="chips">
              @for (ex of exerciseOptions; track ex.value) {
                <label class="chip">
                  <input
                    type="checkbox"
                    [checked]="form.exerciseTypes.includes(ex.value)"
                    (change)="toggleExercise(ex.value)"
                  />
                  <span>{{ ex.label }}</span>
                </label>
              }
            </div>
          </fieldset>
        }

        <fieldset class="types">
          <legend>Grades</legend>
          <div class="chips">
            @for (g of grades(); track g.id) {
              <label class="chip">
                <input
                  type="checkbox"
                  [checked]="form.gradeIds.includes(g.id)"
                  (change)="toggleGrade(g.id)"
                />
                <span>{{ g.nameEn }}</span>
              </label>
            }
          </div>
        </fieldset>

        <fieldset class="types">
          <legend>Rule items <span class="opt">(optional filter)</span></legend>
          <div class="chips">
            @for (r of filteredRules(); track r.id) {
              <label class="chip">
                <input
                  type="checkbox"
                  [checked]="form.ruleIds.includes(r.id)"
                  (change)="toggleRule(r.id)"
                />
                <span>{{ r.categoryNameEn }} · {{ r.nameEn }}</span>
              </label>
            }
          </div>
        </fieldset>

        <div class="row2">
          <label>
            Difficulty
            <select [(ngModel)]="form.difficulty">
              <option [ngValue]="null">Any</option>
              <option [ngValue]="1">Easy</option>
              <option [ngValue]="2">Medium</option>
              <option [ngValue]="3">Hard</option>
            </select>
          </label>
          <fieldset class="types prompt-langs">
            <legend>Prompt language</legend>
            <div class="chips">
              <label class="chip">
                <input type="checkbox" [(ngModel)]="form.showEnglish" name="showEnglish" />
                <span>English</span>
              </label>
              <label class="chip">
                <input type="checkbox" [(ngModel)]="form.showTranslation" name="showTranslation" />
                <span>Translation</span>
              </label>
            </div>
            <p class="muted small">Controls what students see on each question. English is selected by default; pick both to show EN + UR.</p>
          </fieldset>
          <label>
            Items
            <select [(ngModel)]="form.itemCount">
              <option [ngValue]="5">5</option>
              <option [ngValue]="8">8</option>
              <option [ngValue]="12">12</option>
              <option [ngValue]="16">16</option>
            </select>
          </label>
          <label class="check"><input type="checkbox" [(ngModel)]="form.isActive" /> Active</label>
        </div>

        <div class="actions">
          <button type="button" (click)="save()" [disabled]="busy()">Save</button>
          <button type="button" class="ghost" (click)="editing.set(false)">Cancel</button>
        </div>
      </section>
    }

    @if (error()) { <p class="error">{{ error() }}</p> }

    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>Layout</th>
          <th>Items</th>
          <th>Active</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (t of tests(); track t.id) {
          <tr>
            <td>
              <strong>{{ t.name }}</strong>
              @if (t.description) {
                <div class="muted small">{{ t.description }}</div>
              }
            </td>
            <td>{{ layoutLabel(t) }}</td>
            <td>{{ t.itemCount }}</td>
            <td>{{ t.isActive ? 'yes' : 'no' }}</td>
            <td class="row-actions">
              <button type="button" class="ghost" (click)="edit(t.id)">Edit</button>
              @if (t.isActive) {
                <button type="button" class="ghost" (click)="deactivate(t)">Deactivate</button>
              }
            </td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styleUrl: './admin-shared.scss',
  styles: [`
    .wide { max-width: 44rem; }
    .row2 {
      display: grid; gap: .75rem;
      grid-template-columns: 1fr;
    }
    @media (min-width: 640px) {
      .row2 { grid-template-columns: repeat(2, 1fr); }
    }
    .types {
      margin: 0; padding: .75rem .9rem;
      border: 1px solid var(--line); border-radius: .45rem;
    }
    .types legend { font-weight: 650; padding: 0 .2rem; font-size: .9rem; }
    .opt { font-weight: 400; color: var(--muted); font-size: .8rem; }
    .chips { display: flex; flex-wrap: wrap; gap: .45rem; margin-top: .5rem; }
    .chip {
      display: inline-flex; align-items: center; gap: .4rem;
      min-height: 2.4rem; padding: .35rem .7rem;
      border: 1px solid var(--line); border-radius: .45rem;
      background: rgba(255,255,255,.75); cursor: pointer; font-weight: 600; font-size: .9rem;
    }
    .chip:has(input:checked) {
      border-color: var(--accent);
      background: rgba(31, 107, 74, 0.12);
    }
    .small { font-size: .85rem; margin-top: .15rem; }
  `]
})
export class AdminTestsPage implements OnInit {
  readonly exerciseOptions = EXERCISE_OPTIONS;
  readonly tests = signal<SavedTestSummary[]>([]);
  readonly grades = signal<GradeDto[]>([]);
  readonly rules = signal<RuleDto[]>([]);
  readonly editing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  form = this.empty();

  readonly filteredRules = computed(() => {
    const selected = new Set(this.form.gradeIds);
    const all = this.rules();
    if (selected.size === 0) return all;
    return all.filter((r) => r.gradeIds.some((id) => selected.has(id)) || r.gradeIds.length === 0);
  });

  constructor(
    private readonly api: SavedTestsService,
    private readonly content: ContentService
  ) {}

  ngOnInit(): void {
    this.reload();
    this.content.listGrades().subscribe({ next: (g) => this.grades.set(g) });
    this.content.listRules().subscribe({ next: (r) => this.rules.set(r) });
  }

  layoutLabel(t: SavedTestSummary): string {
    if (t.layout === 2) {
      return t.dropdownMode === 1 ? 'Grid · rules' : 'Grid · translation';
    }
    return 'Sequential';
  }

  startNew(): void {
    this.form = this.empty();
    this.editing.set(true);
  }

  onLayoutChange(): void {
    if (this.form.layout === 2 && this.form.dropdownMode === 0) {
      this.form.dropdownMode = 1;
    }
    if (this.form.layout === 1) {
      this.form.dropdownMode = 0;
      if (this.form.exerciseTypes.length === 0) {
        this.form.exerciseTypes = [1, 2, 3, 4, 5, 6];
      }
    }
  }

  toggleExercise(v: number): void {
    const set = new Set(this.form.exerciseTypes);
    if (set.has(v)) set.delete(v);
    else set.add(v);
    this.form.exerciseTypes = [...set].sort((a, b) => a - b);
  }

  toggleGrade(id: number): void {
    const set = new Set(this.form.gradeIds);
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.form.gradeIds = [...set];
  }

  toggleRule(id: number): void {
    const set = new Set(this.form.ruleIds);
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.form.ruleIds = [...set];
  }

  edit(id: number): void {
    this.error.set(null);
    this.api.get(id).subscribe({
      next: (t: SavedTest) => {
        this.form = {
          id: t.id,
          name: t.name,
          description: t.description ?? '',
          layout: t.layout,
          dropdownMode: t.dropdownMode,
          exerciseTypes: [...t.exerciseTypes],
          gradeIds: [...t.gradeIds],
          ruleIds: [...t.ruleIds],
          difficulty: t.difficulty,
          sourceLanguage: t.sourceLanguage,
          showEnglish: t.showEnglish,
          showTranslation: t.showTranslation,
          itemCount: t.itemCount,
          isActive: t.isActive
        };
        this.editing.set(true);
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Could not load test.')
    });
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.error.set('Name is required.');
      return;
    }
    if (this.form.gradeIds.length === 0) {
      this.error.set('Select at least one grade.');
      return;
    }
    if (this.form.layout === 1 && this.form.exerciseTypes.length === 0) {
      this.error.set('Select at least one exercise type.');
      return;
    }
    if (!this.form.showEnglish && !this.form.showTranslation) {
      this.error.set('Select at least one prompt language (English and/or Translation).');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    const body: UpsertSavedTestRequest = {
      name: this.form.name.trim(),
      description: this.form.description.trim() || null,
      layout: this.form.layout,
      dropdownMode: this.form.layout === 2 ? this.form.dropdownMode : 0,
      exerciseTypes: this.form.layout === 1 ? this.form.exerciseTypes : [],
      gradeIds: this.form.gradeIds,
      ruleIds: this.form.ruleIds,
      difficulty: this.form.difficulty,
      sourceLanguage: this.form.showEnglish ? 'en' : 'ur',
      showEnglish: this.form.showEnglish,
      showTranslation: this.form.showTranslation,
      itemCount: this.form.itemCount,
      isActive: this.form.isActive
    };
    const req$ = this.form.id
      ? this.api.update(this.form.id, body)
      : this.api.create(body);
    req$.subscribe({
      next: () => {
        this.busy.set(false);
        this.editing.set(false);
        this.reload();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.error ?? 'Could not save test.');
      }
    });
  }

  deactivate(t: SavedTestSummary): void {
    if (!confirm(`Deactivate “${t.name}”? Students will no longer see it.`)) return;
    this.api.deactivate(t.id).subscribe({
      next: () => this.reload(),
      error: (err) => this.error.set(err?.error?.error ?? 'Could not deactivate.')
    });
  }

  private reload(): void {
    this.api.list().subscribe({
      next: (rows) => this.tests.set(rows),
      error: (err) => this.error.set(err?.error?.error ?? 'Could not load tests.')
    });
  }

  private empty() {
    return {
      id: undefined as number | undefined,
      name: '',
      description: '',
      layout: 1,
      dropdownMode: 0,
      exerciseTypes: [1, 2, 3, 4, 5, 6],
      gradeIds: [] as number[],
      ruleIds: [] as number[],
      difficulty: null as number | null,
      sourceLanguage: 'en',
      showEnglish: true,
      showTranslation: false,
      itemCount: 8,
      isActive: true
    };
  }
}
