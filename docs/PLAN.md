# self-learn.uz mobil ilova: reja

Yangilangan: 2026-11-11. Belgilar: ✅ tayyor · 🔜 keyingi · ⬜ rejada

## 1. Maqsad

`h_ch_2` (self-learn.uz) platformasining Android ilovasi: Avalonia 12 + .NET 10, MVVM (CommunityToolkit.Mvvm).
Birinchi versiya faqat `solo_learner` roli uchun: kirish, bosh sahifa, so'z takrorlash (SRS), darslar.
iOS va Browser head'lari loyihada bor, lekin hozir ishlatilmaydi.

## 2. Hozirgi holat

**Ilova.** ✅ Android muhiti (JDK 17, SDK, emulator `SelfLearn_Pixel`), shablon loyiha, login ekrani (hozircha soxta login, serverga ulanmaydi).

**Backend (`../h_ch_2`): JSON API yo'q.**
- `routes/api.php` da faqat `GET /api/user` (`auth:sanctum`). Tekshirilgan branch'larning hammasida shunday.
- Production'da ham: `/api/user` → 401, `/api/login` → 404, `/api/v1/*` → 404 (2026-11-11 tekshirildi).
- Qolgan hamma narsa Inertia (HTML/Vue) qaytaradi, ilova bunday javobni ishlata olmaydi.

Ishni yengillashtiradigan narsalar:
- Sanctum 4 o'rnatilgan, `User` da `HasApiTokens`, `personal_access_tokens` migratsiyasi bor.
- Biznes mantiq servislarda: `LessonProgressService`, `LessonStateMachine`, `LessonContentService`, vocab review servisi, XP va streak. API shularni qayta ishlatadi.
- `VocabBankController` ning `answer`, `storeWord`, `lookupWord` kabi metodlari allaqachon JSON qaytaradi.
- Lokal muhit bor: PHP 8.5, composer, sqlite. Backendni kompyuterda ishga tushirib, production'ga tegmasdan sinash mumkin.

To'siqlar va xavflar:
1. Dars endpointlari (`advance`, `check`, `remediation`, `continue`, `complete`) Inertia `back()`/redirect qaytaradi. Ular uchun alohida JSON controller yoziladi.
2. Yangi `solo_learner` da `onboarding_completed=false` bo'lsa placement testdan o'tishi shart (`RequireOnboarding` redirect qiladi, API'da JSON xato kerak).
3. Telegram obuna to'sig'i (`TELEGRAM_REQUIRE_SUBSCRIPTION`, default `true`) faqat `web` middleware guruhiga ulangan, `api` guruhida yo'q. Ya'ni API orqali bu to'siqni aylanib o'tish mumkin bo'lib qoladi. Mahsulot qarori kerak (5-bosqich). Productionda yoqilganmi, repodan bilib bo'lmaydi.
4. Login cheklovlari: 3 ta xato urinishdan keyin blok (`LoginRequest`), `throttle:login` (10/daqiqa email+IP).
5. 6 ta rol bor. Ilova faqat `solo_learner` ni qo'llaydi, boshqalarga aniq xabar ko'rsatiladi.
6. `h_ch_2` da 378 ta tugallanmagan o'zgarish bor (egasining ishlari). Ularga tegmaslik uchun API alohida git worktree'da yoziladi.
7. Android 9+ oddiy `http` ni bloklaydi. Lokal backend uchun faqat Debug'da ruxsat beriladi.
8. `ApplicationId` hozir `com.CompanyName.SelfLearn.Mobile` (shablon). Play Store'ga chiqqandan keyin o'zgartirib bo'lmaydi, shuning uchun birinchi release'dan oldin tanlanadi.

## 3. Arxitektura

### Backend (h_ch_2, alohida worktree)
- Yangi `/api/v1/...` guruhi, `App\Http\Controllers\Api\V1\*`, API Resource'lar. Mavjud Inertia controller'larga tegilmaydi (veb buzilmasin).
- Auth: Sanctum personal access token (`Authorization: Bearer`). Login token beradi, logout uni o'chiradi.
- Xatolar: Laravel standart JSON (`message`, `errors`) va barqaror `code` maydoni (`role_not_supported`, `onboarding_required`, `telegram_required`).
- Har endpoint uchun PHPUnit feature test (loyihada sqlite `:memory:` sozlangan).

### Ilova
```
Views  →  ViewModels  →  Services (ApiClient, AuthService, TokenStore)  →  Models (DTO)
```
- DI: Microsoft.Extensions.DependencyInjection.
- Navigatsiya: `ShellViewModel` joriy ekranni ushlaydi, `ViewLocator` mos View'ni topadi.
- Token: boshida oddiy fayl (faqat ishlab chiqish uchun), release'dan oldin Android Keystore.
- Server manzili: Debug'da emulator `http://10.0.2.2:8000`, Desktop `http://localhost:8000`; Release'da `https://self-learn.uz`.
- UI matnlari o'zbekcha (vebdagidek).

## 4. Bosqichlar

### 0-bosqich. Poydevor
- ✅ Android muhiti, shablon, login maketi
- ✅ Git repo (`github.com/SharipovIskandar/self-learn-app`, branch `main`)
- ✅ Bu reja
- 🔜 `ApplicationId` va ilova nomini tanlash

### 1-bosqich. Kirish (backend + ilova) 🔜
Backend:
- `POST /api/v1/auth/login` (`email`, `password`, `device_name`) → `token` va `user` (`id`, `name`, `email`, `role`, `onboarding_completed`)
- `POST /api/v1/auth/logout`, `GET /api/v1/me`
- 422 maydon xatolari, 429 limit, 403 `role_not_supported`

Ilova:
- `Services/` va `Models/` papkalari, `MainViewModel` → `LoginViewModel`, DI, `ShellViewModel`
- `ApiClient`, `AuthService`, `TokenStore`
- Login ekrani haqiqiy API'ga ulanadi, xatolar o'zbekcha ko'rsatiladi
- Ilova ochilganda token bo'lsa avtomatik kirish; "Chiqish" tugmasi

Tayyor deyiladi: emulatorda lokal backendga haqiqiy login ishlaydi; noto'g'ri parolda xabar chiqadi; ilova qayta ochilganda tizimda qoladi; chiqish ishlaydi.

### 2-bosqich. Bosh sahifa ⬜
- `GET /api/v1/solo/dashboard`: salomlashuv, XP, daraja, streak, `srs_due`, "Keyingi qadam" (`next_lesson`), CEFR progress
- Pastki navigatsiya: Bosh sahifa · Darslar · So'zlar · Profil; pull-to-refresh

### 3-bosqich. So'z takrorlash (SRS) ⬜
- `GET /api/v1/vocab/practice`, `POST /api/v1/vocab/answer` (mavjud mantiq), so'z qo'shish va qidirish
- Flashcard ekrani: so'z, IPA, tarjima, misol; javob sifati (0-3); sessiya yakuni va XP
- Darslardan oldin qilinadi: JSON'ga tayyor, kichik, kundalik odat uchun muhim.

### 4-bosqich. Darslar ⬜ (eng katta)
- `GET /api/v1/lessons` (CEFR darajalari bo'yicha mavzular va holat), `GET /lessons/next`, `GET /lessons/{slug}`
- `POST /lessons/{slug}/advance | check | remediation | continue | complete`
- Bosqichlar: hook → explain → examples → guided → check → (remediation) → done
- Dars pleyeri. Mashq turlari ko'p (fill_blank, reading_gap_fill, listening_*, speaking_*, writing_*, reorder va boshqalar). Birinchi versiyada eng soddalari, qolganlari uchun "Veb-saytda davom eting" tugmasi. Aniq ro'yxat bosqich boshida `check` kontentidan olinadi.
- Nazariya audiosi (TTS) keyinroq.

### 5-bosqich. Yangi foydalanuvchi oqimi ⬜
- Placement test (onboarding), ro'yxatdan o'tish, parolni tiklash
- Telegram obuna to'sig'i (qaror kerak, 2-bandning 3-xavfiga qarang)
- Google va Telegram orqali kirish (deep link kerak, murakkabroq)

### 6-bosqich. Release ⬜
- Android Keystore, ikonka va splash, imzolash, Play Store ichki test
- API'ni productionga chiqarish (backend egasi), ilova versiyasini tekshirish

### Hozircha qilmaymiz
AI o'qituvchi (SSE), community, teacher/student/org/admin rollari, Goal va roadmap, analytics, leaderboard, to'lov, iOS, Browser.

## 5. Ishlash qoidalari
- 2026-11-11 dan kodni Claude yozadi, egasi kuzatadi. Har qadam kichik: yoziladi, ishga tushiriladi, tekshiriladi.
- `self-learn-app`: har tugagan qadam `main` ga commit va push qilinadi (egasi ruxsat bergan).
- `h_ch_2`: faqat `../h_ch_2-api` worktree'da (branch `feature/mobile-api`). Push va deploy faqat egasining aniq ruxsati bilan.
- Production'ga qarshi sinov yo'q: lokal backend va sqlite.

## 6. Ishga tushirish

Muhit o'zgaruvchilari `~/.zshrc` da: `JAVA_HOME`, `ANDROID_HOME`, PATH'da `platform-tools` va `emulator`.

```bash
emulator -avd SelfLearn_Pixel                                   # emulator (alohida terminalda)
dotnet build SelfLearn.Mobile.Android -t:Run                    # Android'ga yig'ib o'rnatish
dotnet run --project SelfLearn.Mobile.Desktop                   # Desktop'da tez sinash
```

Agar build `XA5300` (Android SDK topilmadi) desa:
`-p:AndroidSdkDirectory=$ANDROID_HOME -p:JavaSdkDirectory=$JAVA_HOME` qo'shing.

## 7. Ochiq savollar
1. Productionda Telegram obuna to'sig'i yoqilganmi? Ilova ham shuni majburlashi kerakmi?
2. Ilovaning yakuniy nomi va `ApplicationId` (masalan `uz.selflearn.app`).
