import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { apiErrorMessage } from '../../core/api/api-error';
import { ProductApiService } from '../../core/api/product-api.service';
import { Product } from '../../core/models/product.models';

@Component({
  selector: 'app-products-page',
  imports: [ReactiveFormsModule],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.css'
})
export class ProductsPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly productApi = inject(ProductApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  readonly form = this.formBuilder.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    balance: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]]
  });

  products: Product[] = [];
  isSubmitting = false;
  isLoading = false;
  feedback = '';
  error = '';

  ngOnInit(): void {
    this.loadProducts();
  }

  submit(): void {
    this.feedback = '';
    this.error = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.isSubmitting = true;
    this.productApi.create({
      code: value.code.trim(),
      description: value.description.trim(),
      balance: value.balance
    }).pipe(
      finalize(() => {
        this.isSubmitting = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: (product) => {
        this.feedback = `Produto ${product.code} cadastrado com sucesso.`;
        this.form.reset({ code: '', description: '', balance: 0 });
        this.loadProducts();
      },
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'Não foi possível cadastrar o produto.');
      }
    });
  }

  isInvalid(controlName: 'code' | 'description' | 'balance'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  private loadProducts(): void {
    this.isLoading = true;
    this.productApi.list().pipe(
      finalize(() => {
        this.isLoading = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: (products) => this.products = products,
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'Não foi possível carregar os produtos.');
      }
    });
  }
}
