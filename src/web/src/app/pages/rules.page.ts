import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService, RuleCategoryDto } from '../services/content.service';

interface ItemForm {
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
      <button type="button" (click)="startNew()">New category</button>
    </div>
    <p class="lede">
      Categories group rule items (e.g. <em>Tense</em> → Simple Present, Present Continuous).
      Sentences and grid dropdowns link to <strong>items</strong>, not categories.
    </p>

    @if (editing()) {
      <section class="form wide">
        <h3>{{ form.id ? 'Edit' : 'New' }} category</h3>
        <div class="row2">
          <label>Code<input [(ngModel)]="form.code" placeholder="TENSE" /></label>
          <label>Sort<input type="number" [(ngModel)]="form.sortOrder" /></label>
        </div>
        <label>Name (EN)<input [(ngModel)]="form.nameEn" required /></label>
        <label>Name (UR)<input [(ngModel)]="form.nameUr" dir="rtl" /></label>
        <label class="check"><input type="checkbox" [(ngModel)]="form.isActive" /> Active</label>

        <fieldset class="items">
          <legend>Items</legend>
          @for (item of form.items; track $index) {
            <div class="item-row">
              <label>Code<input [(ngModel)]="item.code" [name]="'code'+$index" placeholder="PRESENT_SIMPLE" /></label>
              <label>Name EN<input [(ngModel)]="item.nameEn" [name]="'nen'+$index" /></label>
              <label>Name UR<input [(ngModel)]="item.nameUr" [name]="'nur'+$index" dir="rtl" /></label>
              <label>Sort<input type="number" [(ngModel)]="item.sortOrder" [name]="'so'+$index" /></label>
              <label class="check"><input type="checkbox" [(ngModel)]="item.isActive" [name]="'ia'+$index" /> Active</label>
              <button type="button" class="ghost danger" (click)="removeItem($index)">Remove</button>
            </div>
          }
          <button type="button" class="ghost" (click)="addItem()">Add item</button>
        </fieldset>

        <div class="actions">
          <button type="button" (click)="save()" [disabled]="busy()">Save</button>
          <button type="button" class="ghost" (click)="editing.set(false)">Cancel</button>
        </div>
      </section>
    }

    @if (error()) { <p class="error">{{ error() }}</p> }

    @for (c of categories(); track c.id) {
      <section class="cat">
        <div class="cat-head">
          <div>
            <h3>{{ c.nameEn }} <span class="muted" dir="rtl">{{ c.nameUr }}</span></h3>
            <p class="muted small">{{ c.code || '—' }} · {{ c.isActive ? 'active' : 'inactive' }} · {{ c.items.length }} items</p>
          </div>
          <button type="button" class="ghost" (click)="edit(c)">Edit</button>
        </div>
        <table>
          <thead><tr><th>Code</th><th>Name</th><th>Active</th></tr></thead>
          <tbody>
            @for (i of c.items; track i.id) {
              <tr>
                <td>{{ i.code || '—' }}</td>
                <td>{{ i.nameEn }} <span class="muted" dir="rtl">{{ i.nameUr }}</span></td>
                <td>{{ i.isActive ? 'yes' : 'no' }}</td>
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
  readonly categories = signal<RuleCategoryDto[]>([]);
  readonly editing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  form = this.empty();

  constructor(private readonly api: ContentService) {}

  ngOnInit(): void { this.reload(); }

  startNew(): void {
    this.form = this.empty();
    this.form.items = [this.emptyItem()];
    this.editing.set(true);
  }

  edit(c: RuleCategoryDto): void {
    this.form = {
      id: c.id,
      code: c.code ?? '',
      nameEn: c.nameEn,
      nameUr: c.nameUr ?? '',
      sortOrder: c.sortOrder,
      isActive: c.isActive,
      items: c.items.map((i) => ({
        id: i.id,
        code: i.code ?? '',
        nameEn: i.nameEn,
        nameUr: i.nameUr ?? '',
        explanationEn: '',
        explanationUr: '',
        sortOrder: i.sortOrder,
        isActive: i.isActive
      }))
    };
    this.editing.set(true);
  }

  addItem(): void {
    this.form.items = [...this.form.items, this.emptyItem(this.form.items.length * 10 + 10)];
  }

  removeItem(index: number): void {
    const items = [...this.form.items];
    items.splice(index, 1);
    this.form.items = items;
  }

  save(): void {
    if (!this.form.nameEn.trim()) {
      this.error.set('Category name is required.');
      return;
    }
    const items = this.form.items.filter((i) => i.nameEn.trim());
    if (items.length === 0) {
      this.error.set('Add at least one rule item.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.api.saveRuleCategory({
      nameEn: this.form.nameEn.trim(),
      nameUr: this.form.nameUr.trim() || null,
      code: this.form.code.trim() || null,
      sortOrder: this.form.sortOrder,
      isActive: this.form.isActive,
      items: items.map((i) => ({
        id: i.id ?? null,
        nameEn: i.nameEn.trim(),
        nameUr: i.nameUr.trim() || null,
        explanationEn: i.explanationEn.trim() || null,
        explanationUr: i.explanationUr.trim() || null,
        code: i.code.trim() || null,
        sortOrder: i.sortOrder,
        isActive: i.isActive
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
    this.api.listRuleCategories().subscribe({
      next: (rows) => this.categories.set(rows),
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
      items: [] as ItemForm[]
    };
  }

  private emptyItem(sortOrder = 0): ItemForm {
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
