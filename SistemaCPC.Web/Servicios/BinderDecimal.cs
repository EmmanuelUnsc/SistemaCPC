using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SistemaCPC.Web.Servicios;

public class BinderDecimal : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext contexto)
    {
        var valor = contexto.ValueProvider.GetValue(contexto.ModelName);

        if (string.IsNullOrWhiteSpace(valor.FirstValue))
        {
            return Task.CompletedTask;
        }

        contexto.ModelState.SetModelValue(contexto.ModelName, valor);
        var texto = valor.FirstValue.Trim().Replace(',', '.');

        if (decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var numero))
        {
            contexto.Result = ModelBindingResult.Success(numero);
        }
        else
        {
            contexto.ModelState.TryAddModelError(contexto.ModelName, "El monto no es un número válido.");
        }

        return Task.CompletedTask;
    }
}

public class ProveedorBinderDecimal : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext contexto) =>
        contexto.Metadata.UnderlyingOrModelType == typeof(decimal) ? new BinderDecimal() : null;
}