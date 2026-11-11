using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace SelfLearn.Mobile.Views;

public partial class MainView : UserControl
{
    private IInputPane? _inputPane;

    public MainView()
    {
        InitializeComponent();
    }

    // Ekran klaviaturasi ochilganda ilova maydoni uning balandligicha kichrayadi. Shunda ScrollViewer ichidagi
    // forma va xabarlar klaviatura ortida qolmaydi (kichik telefonlarda ham). Desktop'da InputPane yo'q.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _inputPane = TopLevel.GetTopLevel(this)?.InputPane;
        if (_inputPane is not null)
        {
            _inputPane.StateChanged += OnInputPaneStateChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_inputPane is not null)
        {
            _inputPane.StateChanged -= OnInputPaneStateChanged;
            _inputPane = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        var keyboardHeight = e.NewState == InputPaneState.Open ? e.EndRect.Height : 0;
        Padding = new Thickness(0, 0, 0, keyboardHeight);
    }
}
