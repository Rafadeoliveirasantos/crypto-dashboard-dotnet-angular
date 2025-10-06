using FluentValidation;
using CryptoDashboard.Dto.Crypto;

namespace CryptoDashboard.Application.Validators
{
    public class SettingsDtoValidator : AbstractValidator<SettingsDto>
    {
        public SettingsDtoValidator()
        {
            RuleFor(x => x.UpdateIntervalSeconds)
                .GreaterThan(0).WithMessage("Intervalo deve ser maior que zero.");

            RuleFor(x => x.DefaultCurrency)
                .NotEmpty().WithMessage("Moeda padrão obrigatória.")
                .Length(3).WithMessage("Código da moeda deve ter 3 caracteres.");
        }
    }
}