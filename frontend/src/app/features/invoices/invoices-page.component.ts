import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, FormGroupDirective, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { apiErrorMessage } from '../../core/api/api-error';
import { InvoiceApiService } from '../../core/api/invoice-api.service';
import { ProductApiService } from '../../core/api/product-api.service';
import { InvoiceStatus, InvoiceSummary } from '../../core/models/invoice.models';
import { Product } from '../../core/models/product.models';

@Component({
  selector: 'app-invoices-page',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './invoices-page.component.html',
  styleUrl: './invoices-page.component.css'
})
export class InvoicesPageComponent implements OnInit {
  @ViewChild(FormGroupDirective) private formDirective!: FormGroupDirective;

  private readonly formBuilder = inject(FormBuilder);
  private readonly productApi = inject(ProductApiService);
  private readonly invoiceApi = inject(InvoiceApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  readonly form = this.formBuilder.group({
    items: this.formBuilder.array([this.createItemGroup()])
  }, { validators: this.uniqueProductsValidator() });

  products: Product[] = [];
  invoices: InvoiceSummary[] = [];
  isLoading = false;
  isSubmitting = false;
  feedback = '';
  error = '';
  createdInvoiceNumber: number | null = null;

  get items(): FormArray {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    this.loadPage();
  }

  addItem(): void {
    this.items.push(this.createItemGroup());
    this.form.updateValueAndValidity();
  }

  removeItem(index: number): void {
    if (this.items.length === 1) {
      return;
    }

    this.items.removeAt(index);
    this.form.updateValueAndValidity();
  }

  submit(): void {
    this.feedback = '';
    this.error = '';
    this.createdInvoiceNumber = null;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const requestItems = this.items.controls.map(control => ({
      productId: Number(control.get('productId')?.value),
      quantity: Number(control.get('quantity')?.value)
    }));

    this.isSubmitting = true;
    this.invoiceApi.create({ items: requestItems }).pipe(
      finalize(() => {
        this.isSubmitting = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: (invoice) => {
        this.createdInvoiceNumber = invoice.number;
        this.feedback = `Nota fiscal nº ${invoice.number} criada como Aberta.`;
        this.resetItems();
        this.loadInvoices();
      },
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'Não foi possível criar a nota fiscal.');
      }
    });
  }

  statusLabel(status: InvoiceStatus): string {
    return status === 'Open' ? 'Aberta' : 'Fechada';
  }

  rowInvalid(row: AbstractControl, controlName: string): boolean {
    const control = row.get(controlName);
    return Boolean(control?.invalid && (control.touched || control.dirty));
  }

  private createItemGroup(): FormGroup {
    return this.formBuilder.group({
      productId: [null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]]
    });
  }

  private uniqueProductsValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const formArray = control.get('items') as FormArray | null;
      if (!formArray) {
        return null;
      }

      const ids = formArray.controls
        .map(item => item.get('productId')?.value)
        .filter(id => id !== null && id !== undefined && id !== '');
      return new Set(ids).size === ids.length ? null : { duplicateProducts: true };
    };
  }

  private resetItems(): void {
    while (this.items.length > 1) {
      this.items.removeAt(this.items.length - 1);
    }

    this.formDirective.resetForm({
      items: [{ productId: null, quantity: 1 }]
    });
    this.form.updateValueAndValidity();
  }

  private loadPage(): void {
    this.isLoading = true;
    forkJoin({
      products: this.productApi.list(),
      invoices: this.invoiceApi.list()
    }).pipe(
      finalize(() => {
        this.isLoading = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: ({ products, invoices }) => {
        this.products = products;
        this.invoices = invoices;
      },
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'Não foi possível carregar os dados para emissão.');
      }
    });
  }

  private loadInvoices(): void {
    this.invoiceApi.list().subscribe({
      next: (invoices) => {
        this.invoices = invoices;
        this.changeDetector.markForCheck();
      },
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'A nota foi criada, mas não foi possível atualizar a listagem.');
        this.changeDetector.markForCheck();
      }
    });
  }
}
