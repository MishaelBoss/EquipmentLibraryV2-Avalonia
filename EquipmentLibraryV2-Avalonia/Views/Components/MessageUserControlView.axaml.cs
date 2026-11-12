using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class MessageUserControlView : UserControl
{
    private static readonly TransformOperations HiddenTransform =
        TransformOperations.Parse("translateY(8px)");

    private static readonly TransformOperations ShownTransform =
        TransformOperations.Parse("translateY(0px)");

    private static readonly TransformOperations ClosingTransform =
        TransformOperations.Parse("translateY(-6px)");

    private MessageUserControlViewModel? _viewModel;

    public MessageUserControlView()
    {
        InitializeComponent();

        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        EnsureViewModelSubscribed();

        if (_viewModel?.IsClosing == true)
            return;

        // Начальное состояние выставляется без transition, чтобы появление
        // анимировалось от него, а не наоборот.
        var transitions = Transitions;
        Transitions = null;

        Opacity = 0;
        RenderTransform = HiddenTransform;

        Transitions = transitions;

        Dispatcher.UIThread.Post(() =>
        {
            Opacity = 1;
            RenderTransform = ShownTransform;
        }, DispatcherPriority.Background);
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = null;

        // Одна и та же ViewModel может показываться и в попапе, и в истории,
        // поэтому после переиспользования контейнера сбрасываем состояние.
        var transitions = Transitions;
        Transitions = null;
        Opacity = 1;
        RenderTransform = ShownTransform;
        Transitions = transitions;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        EnsureViewModelSubscribed();
    }

    private void EnsureViewModelSubscribed()
    {
        var viewModel = DataContext as MessageUserControlViewModel;

        if (ReferenceEquals(viewModel, _viewModel))
            return;

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MessageUserControlViewModel.IsClosing))
            return;

        if (_viewModel?.IsClosing != true)
            return;

        Opacity = 0;
        RenderTransform = ClosingTransform;
    }
}
