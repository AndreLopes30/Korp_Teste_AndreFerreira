import { Routes } from '@angular/router';
import { ProductsPageComponent } from './features/products/products-page.component';
import { InvoiceDetailPageComponent } from './features/invoices/invoice-detail-page.component';
import { InvoicesPageComponent } from './features/invoices/invoices-page.component';

export const routes: Routes = [
  { path: 'products', component: ProductsPageComponent, title: 'Produtos | KORP' },
  { path: 'invoices', component: InvoicesPageComponent, title: 'Notas fiscais | KORP' },
  { path: 'invoices/:number', component: InvoiceDetailPageComponent, title: 'Detalhe da nota | KORP' },
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: '**', redirectTo: 'products' }
];
