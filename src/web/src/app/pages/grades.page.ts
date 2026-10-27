import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService, GradeDto, RuleDto } from '../services/content.service';

@Component({
  standalone: true,
  selector: 'app-grades-page',
  imports: [FormsModule],
  template: `
    <div class="head">
      <h2>Grades</h2>
      <button type="button" (click)="startNew()">New grade</button>
    </div>

    @if (editing()) {
      <section class="form">
        <h3>{{ form.id ? 'Edit' : 'New' }} grade</h3>
        <label>Code<input [(ngModel)]="form.code" placeholder="G5" /></label>
        <label>Name (EN)<input [(ngModel)]="form.nameEn" required /></label>
        <label>Name (UR)<input [(ngModel)]="form.nameUr" dir="rtl" /></label>
        <label>Sort order<input type="number" [(ngModel)]="form.sortOrder" /></label>
        <label class="check"><input type="checkbox" [(ngModel)]="form.isActive" /> Active</label>
        <label>Linked rules
          <select multiple [(ngModel)]="form.ruleIds" size="4">
            @for (r of rules(); track r.id) {
              <option [ngValue]="r.id">{{ r.code || r.nameEn }}</option>
            }
          </select>
        </label>
        <div class="actions">
          <button type="button" (click)="save()" [disabled]="busy()">Save</button>
          <button type="button" class="ghost" (click)="editing.set(false)">Cancel</button>
        </div>
      </section>
    }

    @if (error()) { <p class="error">{{ error() }}</p> }

    <table>
      <thead><tr><th>Code</th><th>Name</th><th>Sort</th><th>Active</th><th></th></tr></thead>
      <tbody>
        @for (g of grades(); track g.id) {
          <tr>
            <td>{{ g.code || '—' }}</td>
            <td>{{ g.nameEn }} <span class="muted" dir="rtl">{{ g.nameUr }}</span></td>
            <td>{{ g.sortOrder }}</td>
            <td>{{ g.isActive ? 'yes' : 'no' }}</td>
            <td><button type="button" class="ghost" (click)="edit(g)">Edit</button></td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styleUrl: './admin-shared.scss'
})
export class GradesPage implements OnInit {
  readonly grades = signal<GradeDto[]>([]);
  readonly rules = signal<RuleDto[]>([]);
  readonly editing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  form: { id?: number; code: string; nameEn: string; nameUr: string; sortOrder: number; isActive: boolean; ruleIds: number[] } = this.empty();

  constructor(private readonly api: ContentService) {}

  ngOnInit(): void {
    this.reload();
    this.api.listRules().subscribe({ next: (r) => this.rules.set(r) });
  }

  startNew(): void {
    this.form = this.empty();
    this.editing.set(true);
  }

  edit(g: GradeDto): void {
    this.form = {
      id: g.id,
      code: g.code ?? '',
      nameEn: g.nameEn,
      nameUr: g.nameUr ?? '',
      sortOrder: g.sortOrder,
      isActive: g.isActive,
      ruleIds: [...g.ruleIds]
    };
    this.editing.set(true);
  }

  save(): void {
    this.busy.set(true);
    this.error.set(null);
    const body = {
      nameEn: this.form.nameEn,
      nameUr: this.form.nameUr || null,
      code: this.form.code || null,
      sortOrder: this.form.sortOrder,
      isActive: this.form.isActive
    };
    this.api.saveGrade(body, this.form.id).subscribe({
      next: (saved) => {
        this.api.setGradeRules(saved.id, this.form.ruleIds).subscribe({
          next: () => {
            this.busy.set(false);
            this.editing.set(false);
            this.reload();
          },
          error: (e) => this.fail(e)
        });
      },
      error: (e) => this.fail(e)
    });
  }

  private reload(): void {
    this.api.listGrades().subscribe({
      next: (g) => this.grades.set(g),
      error: (e) => this.error.set(e?.error?.error ?? 'Failed to load grades.')
    });
  }

  private fail(e: { error?: { error?: string } }): void {
    this.busy.set(false);
    this.error.set(e?.error?.error ?? 'Save failed.');
  }

  private empty() {
    return { code: '', nameEn: '', nameUr: '', sortOrder: 0, isActive: true, ruleIds: [] as number[] };
  }
}
