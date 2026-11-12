using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace SelfLearn.Mobile.Views;

public partial class MainView : UserControl
{
    private IInputPane? _inputPane;
    private IInsetsManager? _insets;
    private Thickness _safeArea;
    private double _keyboardHeight;

    public MainView()
    {
        InitializeComponent();
    }

    // Telefonda ilova butun ekranga chiziladi (holat paneli va navigatsiya chizig'i ostiga ham). Shu sababli:
    //  - xavfsiz hudud (holat paneli, navigatsiya chizig'i, kesik/notch) bo'yicha bo'sh joy qoldiramiz;
    //  - ekran klaviaturasi ochilsa, ilova maydoni uning balandligicha kichrayadi, forma va xabarlar ortida qolmasin.
    // Desktop'da bular yo'q (InputPane va InsetsManager null yoki nol).
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);

        _inputPane = topLevel?.InputPane;
        if (_inputPane is not null)
        {
            _inputPane.StateChanged += OnInputPaneStateChanged;
        }

        _insets = topLevel?.InsetsManager;
        if (_insets is not null)
        {
            _insets.SafeAreaChanged += OnSafeAreaChanged;
            _safeArea = ToDips(_insets.SafeAreaPadding);
        }

        UpdatePadding();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_inputPane is not null)
        {
            _inputPane.StateChanged -= OnInputPaneStateChanged;
            _inputPane = null;
        }

        if (_insets is not null)
        {
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
            _insets = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        _keyboardHeight = e.NewState == InputPaneState.Open ? e.EndRect.Height : 0;
        UpdatePadding();
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        _safeArea = ToDips(e.SafeAreaPadding);
        UpdatePadding();
    }

    // Android'da (Avalonia 12.1.5) xavfsiz hudud avval masshtab=1 bilan (fizik piksel, Pixel 7: tepa 136), ishga tushish
    // tugagach esa haqiqiy masshtab bilan (DIP: 51.8) qayta yuboriladi. Har bir qiymatni hisoblangan masshtabi bo'yicha
    // normallashtiramiz: asl piksel = qiymat * hisoblash masshtabi, DIP = piksel / haqiqiy ekran zichligi (Pixel 7: 2.625).
    private Thickness ToDips(Thickness reported)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var computedWith = topLevel?.RenderScaling ?? 1;
        var density = topLevel?.Screens?.ScreenFromVisual(this)?.Scaling ?? computedWith;
        var k = computedWith / density;


        return new Thickness(reported.Left * k, reported.Top * k, reported.Right * k, reported.Bottom * k);
    }

    private void UpdatePadding()
    {
        // Klaviatura ochiqligida pastki bo'shliq klaviaturaning balandligi (u navigatsiya chizig'ini ham qoplaydi).
        Padding = new Thickness(_safeArea.Left, _safeArea.Top, _safeArea.Right, Math.Max(_safeArea.Bottom, _keyboardHeight));
    }
}
