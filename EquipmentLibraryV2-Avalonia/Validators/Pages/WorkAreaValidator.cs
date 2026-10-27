using FluentValidation;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages; 

namespace EquipmentLibraryV2_Avalonia.Validators.Pages;

public class WorkAreaValidator : AbstractValidator<WorkAreaUserControlViewModel>
{
    public WorkAreaValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Наименование объекта не может быть пустым")
            .MinimumLength(3).WithMessage("Наименование должно содержать минимум 3 символа");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Необходимо указать модель изделия");

        RuleFor(x => x.SerialNumber)
            .NotEmpty().WithMessage("Для Средств Измерений (СИ) заводской номер является обязательным!")
            .When(x => x.SelectedEquipmentType?.Id == 1); 
        
        RuleFor(x => x.InvNumber)
            .NotEmpty().WithMessage("Введите инвентарный номер объекта");
    }
}