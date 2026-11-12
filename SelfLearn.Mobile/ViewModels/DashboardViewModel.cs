using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfLearn.Mobile.Models;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.ViewModels;

/// <summary>Bosh sahifa: daraja, XP, ketma-ket kunlar, til darajasi, takrorlash va keyingi dars.</summary>
public partial class DashboardViewModel : TabViewModelBase
{
    private readonly DashboardService _dashboard;
    private readonly AuthService _auth;

    public DashboardViewModel(DashboardService dashboard, AuthService auth)
    {
        _dashboard = dashboard;
        _auth = auth;
    }

    public override string Title => "Bosh sahifa";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = "";

    /// <summary>Daraja aniqlash testi ilovada hali yo'q (5-bosqich), shuning uchun veb-saytga yo'naltiramiz.</summary>
    [ObservableProperty]
    public partial bool NeedsPlacementTest { get; set; }

    [ObservableProperty]
    public partial bool HasData { get; set; }

    [ObservableProperty]
    public partial string GreetingText { get; set; } = "";

    [ObservableProperty]
    public partial string LevelText { get; set; } = "";

    [ObservableProperty]
    public partial double LevelProgress { get; set; }

    [ObservableProperty]
    public partial string XpText { get; set; } = "";

    [ObservableProperty]
    public partial string StreakText { get; set; } = "";

    [ObservableProperty]
    public partial string CefrText { get; set; } = "";

    [ObservableProperty]
    public partial double CefrMastery { get; set; }

    [ObservableProperty]
    public partial string SrsText { get; set; } = "";

    [ObservableProperty]
    public partial bool HasNextLesson { get; set; }

    [ObservableProperty]
    public partial string NextLessonTitle { get; set; } = "";

    [ObservableProperty]
    public partial string NextLessonMeta { get; set; } = "";

    /// <summary>Ma'lumotni serverdan yuklaydi. Qayta chaqirilsa (yangilash) eski ma'lumot ko'rinib turadi.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        ErrorMessage = "";

        if (_auth.CurrentUser is { OnboardingCompleted: false })
        {
            NeedsPlacementTest = true;
            HasData = false;
            return;
        }

        NeedsPlacementTest = false;
        IsLoading = true;

        try
        {
            Apply(await _dashboard.GetAsync());
        }
        catch (ApiException ex) when (ex.Code == "onboarding_required")
        {
            NeedsPlacementTest = true;
            HasData = false;
        }
        catch (ApiException ex)
        {
            // Eski ma'lumot (bo'lsa) ko'rinib turadi, ustida xato xabari chiqadi.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(DashboardResponse data)
    {
        GreetingText = $"{data.Greeting}, {data.User.Name}!";
        LevelText = $"{data.User.Level}-daraja · {data.User.LevelName}";
        LevelProgress = data.User.LevelProgress;
        XpText = $"{data.User.XpPoints} XP";
        StreakText = data.User.StreakDays == 0
            ? "Bugun boshlang!"
            : $"{data.User.StreakDays} kun ketma-ket";

        CefrText = data.Cefr.Next is null
            ? $"Til darajasi: {data.Cefr.Current}"
            : $"Til darajasi: {data.Cefr.Current} → {data.Cefr.Next}";
        CefrMastery = data.Cefr.MasteryPct;

        SrsText = data.Srs.Due > 0
            ? $"{data.Srs.Due} ta so'zni takrorlash vaqti keldi"
            : data.Srs.Total == 0
                ? "Lug'atingiz hali bo'sh"
                : "Bugun takrorlash kerak emas";

        HasNextLesson = data.NextLesson is not null;
        NextLessonTitle = data.NextLesson?.Title ?? "";
        NextLessonMeta = data.NextLesson is null
            ? ""
            : $"{data.NextLesson.Level} · {(data.NextLesson.Resume ? "davom ettirish" : "yangi dars")}";

        HasData = true;
    }
}
