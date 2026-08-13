export type InvoiceStatus = 'Open' | 'Closed';

export interface CreateInvoiceItemRequest {
  productId: number;
  quantity: number;
}

export interface CreateInvoiceRequest {
  items: CreateInvoiceItemRequest[];
}

export interface InvoiceItem {
  productId: number;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface InvoiceSummary {
  number: number;
  status: InvoiceStatus;
  createdAtUtc: string;
  closedAtUtc: string | null;
  itemCount: number;
}

export interface InvoiceDetail {
  number: number;
  status: InvoiceStatus;
  createdAtUtc: string;
  closedAtUtc: string | null;
  items: InvoiceItem[];
}
