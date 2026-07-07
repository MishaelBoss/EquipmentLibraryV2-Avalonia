using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EquipmentLibraryV2_Avalonia.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EquipmentLibraryV2_Avalonia
{
    /// <summary>
    /// Given a view model, returns the corresponding view if possible.
    /// </summary>
    [RequiresUnreferencedCode(
        "Default implementation of ViewLocator involves reflection which may be trimmed away.",
        Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            // ReSharper disable once NullableWarningSuppressionIsUsed
            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null)
            {
                // ReSharper disable once NullableWarningSuppressionIsUsed
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
