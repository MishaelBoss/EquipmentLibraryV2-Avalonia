using EquipmentLibraryV2_Avalonia.ViewModels;
using FluentValidation;

namespace EquipmentLibraryV2_Avalonia.Validators;

public class CreateAdminValidator : AbstractValidator<CreateAdminDialogWindowViewModel>
{
    public CreateAdminValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин не может быть пустым");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль не может быть пустым")
            .MinimumLength(6).WithMessage("Пароль должно содержать минимум 6 символов");
        
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Повторите пароль")
            .Equal(x => x.Password).WithMessage("Пароли не совпадают");
    }
}