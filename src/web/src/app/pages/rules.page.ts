import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService, RuleDto } from '../services/content.service';

interface DetailForm {
  id?: number;
  code: string;
  nameEn: string;
  nameUr: string;
  explanationEn: string;
  explanationUr: string;
  sortOrder: number;
  isActive: boolean;
}

@Component({
  standalone: true,
  selector: 'app-rules-page',
  imports: [FormsModule],
  template: `
    <div class="head">
      <h2>Rules</h2>
      <button type="button" (click)="startNew()">New rule</button>
    </div>
    <p class="lede">
      Each <em>Rule</em> is a master (e.g. Types of Speech, Tense) with detail rows
      (Direct Speech, Indirect Speech). Sentences and recognition dropdowns link to
      <strong>details</strong>.
    </p>

    @if (editing()) {
      <section class="form wide">
        <h3>{{ form.id ? 'Edit' : 'New' }} rule</h3>
        <div class="row2">
          <label>Code<input [(ngModel)]="form.code" placeholder="TENSE" /></label>
          <label>Sort<input type="number" [(ngModel)]="form.sortOrder" /></label>
        </div>
        <label>Name (EN)<input [(ngModel)]="form.nameEn" required /></label>
        <label>Name (UR)<input [(ngModel)]="form.nameUr" dir="rtl" /></label>
        <label class="check"><input type="checkbox" [(ngModel)]="form.isActive" /> Active</label>

        <fieldset class="items">
          <legend>Details</legend>
          @for (item of form.details; track $index) {
            <div class="item-row">
              <label>Code<input [(ngModel)]="item.code" [name]="'code'+$index" placeholder="PRESENT_SIMPLE" /></label>
              <label>Name EN<input [(ngModel)]="item.nameEn" [name]="'nen'+$index" /></label>
              <label>Name UR<input [(ngModel)]="item.nameUr" [name]="'nur'+$index" dir="rtl" /></label>
              <label>Sort<input type="number" [(ngModel)]="item.sortOrder" [name]="'so'+$index" /></label>
              <label class="check"><input type="checkbox" [(ngModel)]="item.isActive" [name]="'ia'+$index" /> Active</label>
              <button type="button" class="ghost danger" (click)="removeDetail($index)">Remove</button>
            </div>
          }
          <button type="button" class="ghost" (click)="addDetail()">Add detail</button>
        </fieldset>

        <div class="actions">
          <button type="button" (click)="save()" [disabled]="busy()">Save</button>
          <button type="button" class="ghost" (click)="editing.set(false)">Cancel</button>
        </div>
      </section>
    }

    @if (error()) { <p class="error">{{ error() }}</p> }

    @for (r of rules(); track r.id) {
      <section class="cat">
        <div class="cat-head">
          <div>
            <h3>{{ r.nameEn }} <span class="muted" dir="rtl">{{ r.nameUr }}</span></h3>
            <p class="muted small">{{ r.code || '—' }} · {{ r.isActive ? 'active' : 'inactive' }} · {{ r.details.length }} details</p>
          </div>
          <button type="button" class="ghost" (click)="edit(r)">Edit</button>
        </div>
        <table>
          <thead><tr><th>Code</th><th>Name</th><th>Active</th></tr></thead>
          <tbody>
            @for (d of r.details; track d.id) {
              <tr>
                <td>{{ d.code || '—' }}</td>
                <td>{{ d.nameEn }} <span class="muted" dir="rtl">{{ d.nameUr }}</span></td>
                <td>{{ d.isActive ? 'yes' : 'no' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </section>
    }
  `,
  styleUrl: './admin-shared.scss',
  styles: [`
    .wide { max-width: 48rem; }
    .lede { color: var(--muted); max-width: 40rem; }
    .row2 { display: grid; gap: .75rem; grid-template-columns: 1fr 1fr; }
    .items {
      margin: .75rem 0 0; padding: .75rem .9rem;
      border: 1px solid var(--line); border-radius: .45rem;
    }
    .item-row {
      display: grid; gap: .5rem; grid-template-columns: 1fr;
      padding: .6rem 0; border-bottom: 1px solid var(--line);
    }
    @media (min-width: 720px) {
      .item-row { grid-template-columns: 1fr 1.2fr 1.2fr 5rem auto auto; align-items: end; }
    }
    .cat { margin: 1.25rem 0 1.75rem; }
    .cat-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: .5rem; }
    .cat-head h3 { margin: 0; font-size: 1.1rem; }
    .small { font-size: .85rem; margin: .2rem 0 0; }
    .danger { color: #a11; }
  `]
})
export class RulesPage implements OnInit {
  readonly rules = signal<RuleDto[]>([]);
  readonly editing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  form = this.empty();

  constructor(private readonly api: ContentService) {}

  ngOnInit(): void { this.reload(); }

  startNew(): void {
    this.form = this.empty();
    this.form.details = [this.emptyDetail()];
    this.editing.set(true);
  }

  edit(r: RuleDto): void {
    this.form = {
      id: r.id,
      code: r.code ?? '',
      nameEn: r.nameEn,
      nameUr: r.nameUr ?? '',
      sortOrder: r.sortOrder,
      isActive: r.isActive,
      details: r.details.map((d) => ({
        id: d.id,
        code: d.code ?? '',
        nameEn: d.nameEn,
        nameUr: d.nameUr ?? '',
        explanationEn: '',
        explanationUr: '',
        sortOrder: d.sortOrder,
        isActive: d.isActive
      }))
    };
    this.editing.set(true);
  }

  addDetail(): void {
    this.form.details = [...this.form.details, this.emptyDetail(this.form.details.length * 10 + 10)];
  }

  removeDetail(index: number): void {
    const details = [...this.form.details];
    details.splice(index, 1);
    this.form.details = details;
  }

  save(): void {
    if (!this.form.nameEn.trim()) {
      this.error.set('Rule name is required.');
      return;
    }
    const details = this.form.details.filter((d) => d.nameEn.trim());
    if (details.length === 0) {
      this.error.set('Add at least one rule detail.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.api.saveRule({
      nameEn: this.form.nameEn.trim(),
      nameUr: this.form.nameUr.trim() || null,
      code: this.form.code.trim() || null,
      sortOrder: this.form.sortOrder,
      isActive: this.form.isActive,
      details: details.map((d) => ({
        id: d.id ?? null,
        nameEn: d.nameEn.trim(),
        nameUr: d.nameUr.trim() || null,
        explanationEn: d.explanationEn.trim() || null,
        explanationUr: d.explanationUr.trim() || null,
        code: d.code.trim() || null,
        sortOrder: d.sortOrder,
        isActive: d.isActive
      }))
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

  private reload(): void {
    this.api.listRules().subscribe({
      next: (rows) => this.rules.set(rows),
      error: (e) => this.error.set(e?.error?.error ?? 'Could not load rules.')
    });
  }

  private empty() {
    return {
      id: undefined as number | undefined,
      code: '',
      nameEn: '',
      nameUr: '',
      sortOrder: 0,
      isActive: true,
      details: [] as DetailForm[]
    };
  }

  private emptyDetail(sortOrder = 0): DetailForm {
    return {
      code: '',
      nameEn: '',
      nameUr: '',
      explanationEn: '',
      explanationUr: '',
      sortOrder,
      isActive: true
    };
  }
}
