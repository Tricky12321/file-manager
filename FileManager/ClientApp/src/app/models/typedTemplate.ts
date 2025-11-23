import {Directive, inject, input, Input, TemplateRef} from '@angular/core';

@Directive({selector: 'ng-template[typedTemplate]'})
export class TypedTemplateDirective<TypeToken> {

  // how you tell the directive what the type should be
  readonly typeToken = input.required<TypeToken>({ alias: 'typedTemplate' });

  // the directive gets the template from Angular
  private readonly contentTemplate = inject(TemplateRef<TypeToken>);

  // this magic is how we tell Angular the context type for this directive, which then propagates down to the type of the template
  static ngTemplateContextGuard<TypeToken>(dir: TypedTemplateDirective<TypeToken>, ctx: unknown): ctx is TypeToken{ return true; }
}
