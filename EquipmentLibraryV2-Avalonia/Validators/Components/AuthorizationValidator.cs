using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using FluentValidation;

namespace EquipmentLibraryV2_Avalonia.Validators.Components;

public class AuthorizationValidator : AbstractValidator<AuthorizationUserControlViewModel>
{
    public AuthorizationValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин не может быть пустым");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль не может быть пустым")
            .MinimumLength(6).WithMessage("Пароль должно содержать минимум 6 символов");
    }
}