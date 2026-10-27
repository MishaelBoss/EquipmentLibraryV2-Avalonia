using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using FluentValidation;

namespace EquipmentLibraryV2_Avalonia.Validators.Components;

public class AddOrEditUserValidator : AbstractValidator<AddOrEditUserUserControlViewModel>
{
    public AddOrEditUserValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин пользователя обязателен для заполнения")
            .MinimumLength(3).WithMessage("Логин должен содержать не менее 3 символов");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Имя сотрудника обязательно для заполнения");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Фамилия сотрудника обязательна для заполнения");

        RuleFor(x => x.SelectUserRole)
            .NotNull().WithMessage("Необходимо выбрать роль пользователя в системе");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен при регистрации нового аккаунта")
            .MinimumLength(6).WithMessage("Пароль должен быть не короче 6 символов")
            .When(x => x.IsNewUser);

        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("Новый пароль должен быть не короче 6 символов")
            .When(x => !x.IsNewUser && !string.IsNullOrEmpty(x.Password) && x.Password != "********");
    }
}