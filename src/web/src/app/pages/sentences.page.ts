import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService, GradeDto, RuleDto, SentenceDto } from '../services/content.service';

@Component({
  standalone: true,
  selector: 'app-sentences-page',
  imports: [FormsModule],
  template: `
    <div class="head">
      <h2>Sentences</h2>
      <button type="button" (click)="startNew()">New sentence</button>
    </div>

    <section class="filters">
      <label>Grade
        <select [(ngModel)]="filterGradeId" (ngModelChange)="reload()">
          <option [ngValue]="undefined">All</option>
          @for (g of grades(); track g.id) {
            <option [ngValue]="g.id">{{ g.nameEn }}</option>
          }
        </select>
      </label>
      <label>Status
        <select [(ngModel)]="filterStatus" (ngModelChange)="reload()">
          <option [ngValue]="undefined">All</option>
          <option [ngValue]="0">Draft</option>
          <option [ngValue]="1">Approved</option>
        </select>
      </label>
      <label>Difficulty
        <select [(ngModel)]="filterDifficulty" (ngModelChange)="reload()">
          <option [ngValue]="undefined">All</option>
          <option [ngValue]="1">Easy</option>
          <option [ngValue]="2">Medium</option>
          <option [ngValue]="3">Hard</option>
        </select>
      </label>
    </section>

    @if (editing()) {
      <section class="form">
        <h3>{{ form.id ? 'Edit' : 'New' }} sentence</h3>
        <label>English<textarea [(ngModel)]="form.textEn" rows="2"></textarea></label>
        <label>Urdu<textarea [(ngModel)]="form.textUr" rows="2" dir="rtl"></textarea></label>
        <label>Grade
          <select [(ngModel)]="form.gradeId">
            @for (g of grades(); track g.id) {
              <option [ngValue]="g.id">{{ g.nameEn }}</option>
            }
          </select>
        </label>
        <label>Difficulty
          <select [(ngModel)]="form.difficulty">
            <option [ngValue]="1">Easy</option>
            <option [ngValue]="2">Medium</option>
            <option [ngValue]="3">Hard</option>
          </select>
        </label>
        <label>Status
          <select [(ngModel)]="form.status">
            <option [ngValue]="0">Draft</option>
            <option [ngValue]="1">Approved</option>
          </select>
        </label>
        <label>Rules
          <select multiple [(ngModel)]="form.ruleIds" size="4">
            @for (r of rules(); track r.id) {
              <option [ngValue]="r.id">{{ r.code || r.nameEn }}</option>
            }
          </select>
        </label>
        <div class="actions">
          <button type="button" (click)="save()" [disabled]="busy()">Save</button>
          @if (form.id) {
            <button type="button" class="ghost" (click)="suggestMt()" [disabled]="busy()">MT suggest (stub)</button>
          }
          <button type="button" class="ghost" (click)="editing.set(false)">Cancel</button>
        </div>
        @if (mtHint()) {
          <p class="hint">{{ mtHint() }}
            <button type="button" class="ghost" (click)="acceptMt()">Accept into fields</button>
          </p>
        }
      </section>
    }

    @if (error()) { <p class="error">{{ error() }}</p> }

    <table>
      <thead>
        <tr><th>EN</th><th>UR</th><th>Grade</th><th>Diff</th><th>Status</th><th>Rules</th><th></th></tr>
      </thead>
      <tbody>
        @for (s of sentences(); track s.id) {
          <tr>
            <td>{{ s.textEn }}</td>
            <td dir="rtl">{{ s.textUr }}</td>
            <td>{{ s.gradeNameEn }}</td>
            <td>{{ diffLabel(s.difficulty) }}</td>
            <td>{{ s.status === 1 ? 'Approved' : 'Draft' }}</td>
            <td>{{ s.ruleCodes.join(', ') }}</td>
            <td class="row-actions">
              <button type="button" class="ghost" (click)="edit(s)">Edit</button>
              @if (s.status !== 1) {
                <button type="button" class="ghost" (click)="approve(s, true)">Approve</button>
              } @else {
                <button type="button" class="ghost" (click)="approve(s, false)">Unapprove</button>
              }
            </td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styleUrl: './admin-shared.scss'
})
export class SentencesPage implements OnInit {
  readonly grades = signal<GradeDto[]>([]);
  readonly rules = signal<RuleDto[]>([]);
  readonly sentences = signal<SentenceDto[]>([]);
  readonly editing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly mtHint = signal<string | null>(null);

  filterGradeId: number | undefined;
  filterStatus: number | undefined;
  filterDifficulty: number | undefined;
  form = this.empty();

  constructor(private readonly api: ContentService) {}

  ngOnInit(): void {
    this.api.listGrades().subscribe({
      next: (g) => {
        this.grades.set(g);
        if (!this.form.gradeId && g.length) this.form.gradeId = g[0].id;
      }
    });
    this.api.listRules().subscribe({ next: (r) => this.rules.set(r) });
    this.reload();
  }

  startNew(): void {
    this.form = this.empty();
    if (this.grades().length) this.form.gradeId = this.grades()[0].id;
    this.mtHint.set(null);
    this.editing.set(true);
  }

  edit(s: SentenceDto): void {
    this.form = {
      id: s.id,
      textEn: s.textEn,
      textUr: s.textUr,
      gradeId: s.gradeId,
      difficulty: s.difficulty,
      status: s.status,
      ruleIds: [...s.ruleIds]
    };
    this.mtHint.set(
      s.mtSuggestedUr || s.mtSuggestedEn
        ? `Stored suggestion — EN: ${s.mtSuggestedEn ?? '—'} / UR: ${s.mtSuggestedUr ?? '—'}`
        : null
    );
    this.editing.set(true);
  }

  save(): void {
    this.busy.set(true);
    this.error.set(null);
    this.api.saveSentence({
      textEn: this.form.textEn,
      textUr: this.form.textUr,
      gradeId: this.form.gradeId,
      difficulty: this.form.difficulty,
      status: this.form.status,
      ruleIds: this.form.ruleIds
    }, this.form.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.editing.set(false);
        this.reload();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error ?? 'Save failed.');
      }
    });
  }

  approve(s: SentenceDto, approve: boolean): void {
    this.api.approveSentence(s.id, approve).subscribe({
      next: () => this.reload(),
      error: (e) => this.error.set(e?.error?.error ?? 'Approve failed.')
    });
  }

  suggestMt(): void {
    if (!this.form.id) return;
    this.busy.set(true);
    this.api.suggestMt(this.form.id).subscribe({
      next: (res) => {
        this.busy.set(false);
        this.mtHint.set(`Stub MT (${res.provider}): EN=${res.suggestedEn ?? '—'} / UR=${res.suggestedUr ?? '—'}`);
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error ?? 'MT failed.');
      }
    });
  }

  acceptMt(): void {
    if (!this.form.id) return;
    this.api.acceptMt(this.form.id).subscribe({
      next: (s) => {
        this.form.textEn = s.textEn;
        this.form.textUr = s.textUr;
        this.mtHint.set('Accepted into fields (status unchanged).');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error ?? 'Accept MT failed.')
    });
  }

  reload(): void {
    this.api.listSentences({
      gradeId: this.filterGradeId,
      status: this.filterStatus,
      difficulty: this.filterDifficulty
    }).subscribe({
      next: (s) => this.sentences.set(s),
      error: (e) => this.error.set(e?.error?.error ?? 'Failed to load sentences.')
    });
  }

  diffLabel(d: number): string {
    return d === 2 ? 'Medium' : d === 3 ? 'Hard' : 'Easy';
  }

  private empty() {
    return {
      id: undefined as number | undefined,
      textEn: '',
      textUr: '',
      gradeId: 0,
      difficulty: 1,
      status: 0,
      ruleIds: [] as number[]
    };
  }
}
