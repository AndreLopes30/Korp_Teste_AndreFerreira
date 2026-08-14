import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { apiErrorMessage } from '../../core/api/api-error';
import { InvoiceApiService } from '../../core/api/invoice-api.service';
import { InvoiceDetail, InvoiceStatus } from '../../core/models/invoice.models';

@Component({
  selector: 'app-invoice-detail-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './invoice-detail-page.component.html',
  styleUrl: './invoice-detail-page.component.css'
})
export class InvoiceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly invoiceApi = inject(InvoiceApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  invoice: InvoiceDetail | null = null;
  isLoading = false;
  isClosing = false;
  feedback = '';
  error = '';

  get totalQuantity(): number {
    return this.invoice?.items.reduce((total, item) => total + item.quantity, 0) ?? 0;
  }

  ngOnInit(): void {
    const number = Number(this.route.snapshot.paramMap.get('number'));
    if (!Number.isInteger(number) || number <= 0) {
      this.error = 'Número de nota fiscal inválido.';
      return;
    }

    this.loadInvoice(number);
  }

  closeAndPrint(): void {
    if (!this.invoice || this.invoice.status !== 'Open' || this.isClosing) {
      return;
    }

    this.feedback = '';
    this.error = '';
    this.isClosing = true;
    this.invoiceApi.close(this.invoice.number).pipe(
      finalize(() => {
        this.isClosing = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: (invoice) => {
        this.invoice = invoice;
        this.feedback = 'Nota fiscal fechada e estoque atualizado com sucesso.';
        window.setTimeout(() => window.print());
      },
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'Não foi possível processar a nota fiscal. Ela permanece Aberta.');
      }
    });
  }

  statusLabel(status: InvoiceStatus): string {
    return status === 'Open' ? 'Aberta' : 'Fechada';
  }

  private loadInvoice(number: number): void {
    this.isLoading = true;
    this.invoiceApi.getByNumber(number).pipe(
      finalize(() => {
        this.isLoading = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: (invoice) => this.invoice = invoice,
      error: (requestError) => {
        this.error = apiErrorMessage(requestError, 'Não foi possível carregar a nota fiscal.');
      }
    });
  }
}
