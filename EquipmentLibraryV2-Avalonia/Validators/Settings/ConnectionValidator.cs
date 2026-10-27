using EquipmentLibraryV2_Avalonia.ViewModels.Settings;
using FluentValidation;

namespace EquipmentLibraryV2_Avalonia.Validators.Settings;

public class ConnectionValidator : AbstractValidator<ConnectionUserControlViewModel>
{
    public ConnectionValidator()
    {
        RuleFor(x => x.Ip)
            .NotEmpty().WithMessage("Укажите IP-адрес или хост сервера");
        
        RuleFor(x => x.Port)
            .NotEmpty().WithMessage("Укажите порт подключения")
            .Matches(@"^\d+$").WithMessage("Порт должен состоять только из цифр");
        
        RuleFor(x => x.Database)
            .NotEmpty().WithMessage("Укажите имя базы данных");
                
        RuleFor(x => x.User)
            .NotEmpty().WithMessage("Укажите имя пользователя базы данных");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Укажите пароль от базы данных");
    }
}