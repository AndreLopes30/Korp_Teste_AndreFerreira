import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateInvoiceRequest, InvoiceDetail, InvoiceSummary } from '../models/invoice.models';

@Injectable({ providedIn: 'root' })
export class InvoiceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.billingApiUrl}/invoices`;

  list(): Observable<InvoiceSummary[]> {
    return this.http.get<InvoiceSummary[]>(this.baseUrl);
  }

  getByNumber(number: number): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.baseUrl}/${number}`);
  }

  create(request: CreateInvoiceRequest): Observable<InvoiceDetail> {
    return this.http.post<InvoiceDetail>(this.baseUrl, request);
  }

  close(number: number): Observable<InvoiceDetail> {
    return this.http.post<InvoiceDetail>(`${this.baseUrl}/${number}/close`, null);
  }
}
