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

### API v1 (hozirgi shartnoma)
Hamma `/api/*` javobi JSON (`Accept` sarlavhasiz ham). Himoyalangan endpointlarda `Authorization: Bearer <token>`.

| Endpoint | Muvaffaqiyat | Xatolar |
|---|---|---|
| `POST /api/v1/auth/login` `{email, password, device_name}` | 200 `{token, token_type, user}` | 422 `errors.email` (email yoki parol xato) yoki maydon xatolari; 403 `code: role_not_supported`; 429 `code: too_many_attempts`, `retry_after` (+ `Retry-After`) |
| `POST /api/v1/auth/logout` | 204 | 401 |
| `GET /api/v1/me` | 200 `{user}` | 401 |
| `GET /api/v1/solo/dashboard` | 200 (hosila maydonlar 2-bosqichda) | 401; 403 `role_not_supported`; 409 `onboarding_required` |

- `user` = `{id, name, email, role, onboarding_completed}`.
- 401 faqat "token yo'q yoki yaroqsiz" degani. Login'dagi noto'g'ri parol 422 bo'ladi (ilova 401 ni "chiqib ketish" deb qabul qila oladi).
- Bir qurilmadan qayta kirilsa, shu qurilmaning eski tokeni almashtiriladi. Token muddatsiz, logout'da o'chadi. Logout faqat joriy qurilmaga ta'sir qiladi.
- Email katta harf bilan kiritilsa ham topiladi (telefon klaviaturasi uchun).
- 3 ta xato urinishdan keyin 60 soniya blok (veb bilan bir xil qoida).

## 4. Bosqichlar

### 0-bosqich. Poydevor
- ✅ Android muhiti, shablon, login maketi
- ✅ Git repo (`github.com/SharipovIskandar/self-learn-app`, branch `main`)
- ✅ Bu reja
- ✅ Backend uchun alohida worktree va lokal server (`../h_ch_2-api`, 6-bo'limga qarang)
- 🔜 `ApplicationId` va ilova nomini tanlash

### 1-bosqich. Kirish (backend + ilova) ✅ (2026-11-11)
Backend ✅ (`../h_ch_2-api`, branch `feature/mobile-api`, commit `1a073a4f`, hali push qilinmagan):
- `POST /api/v1/auth/login`, `POST /api/v1/auth/logout`, `GET /api/v1/me` va 14 ta test. Shartnoma: "API v1" bo'limi
- Backendning butun test to'plami o'tdi (1421 ta), veb xatti-harakati buzilmagan

Ilova ✅:
- `Services/` (`ApiClient`, `AuthService`, `FileTokenStore`, `DeviceIdentity`, `ApiOptions`), `Models/`, DI (`ServiceCollectionExtensions`)
- Ekranlar: `Startup` (sessiyani tiklaydi) → `Login` yoki `Home`; navigatsiya `ShellViewModel` orqali
- Xatolar o'zbekcha: noto'g'ri parol, qo'llanmaydigan rol, limit, server yo'q ("Qayta urinish" bilan, tizimdan chiqarmaydi)
- Klaviatura ochilganda forma siljiydi (`MainView` dagi `InputPane`), klaviaturadagi Enter ham kirishni boshlaydi
- 61 ta unit test (`SelfLearn.Mobile.Tests`)

Emulatorda tekshirilgan (Debug va Release): kirish, noto'g'ri parol, o'qituvchi (403), yangi o'quvchi eslatmasi, qayta ochganda avtomatik kirish, chiqish, server o'chiq holat va qayta urinish.


### 2-bosqich. Bosh sahifa ✅ (2026-11-12)
Backend ✅ (`feature/mobile-api`, commit `83a54ce1`, hali push qilinmagan):
- `GET /api/v1/solo/dashboard`: salomlashuv, XP/daraja/streak, CEFR, `srs {due,total}`, `vocab_today`, `next_lesson {slug,title,topic,level,resume}`, `skill_mastery`
- `api.solo` middleware: o'quv endpointlari faqat onboarding'dan o'tgan `solo_learner` uchun, aks holda JSON `403 role_not_supported` / `409 onboarding_required`
- 8 ta test

Ilova ✅:
- Pastki navigatsiya: Bosh sahifa · Darslar · So'zlar · Profil (`MainTabsViewModel`). Darslar va So'zlar hozircha "tez orada" bo'limi
- Bosh sahifa: daraja va XP, ketma-ket kunlar, keyingi dars, takrorlash, til darajasi; pastga tortib yangilash; xatoda eski ma'lumot saqlanadi
- Token serverda yaroqsiz bo'lsa (istalgan so'rovda 401) avtomatik Login'ga qaytadi: "Sessiya tugagan"
- Xavfsiz hudud (holat paneli, navigatsiya chizig'i) va klaviatura hisobga olingan, Debug va Release'da bir xil
- 77 ta unit test

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
dotnet test SelfLearn.Mobile.Tests                              # unit testlar
```

Agar build `XA5300` (Android SDK topilmadi) desa:
`-p:AndroidSdkDirectory=$ANDROID_HOME -p:JavaSdkDirectory=$JAVA_HOME` qo'shing.

### Lokal backend
`../h_ch_2-api` ning o'z `.env` fayli (`.env.example` asosida, kalitlarsiz) va sqlite bazasi bor. Productionga hech narsa ulanmagan.

```bash
cd ../h_ch_2-api
php artisan serve --host=127.0.0.1 --port=8000        # emulatordan: http://10.0.2.2:8000
php artisan db:seed --class=MobileDevSeeder           # sinov foydalanuvchilarini tiklash (faqat local)
php artisan test tests/Feature/Api                    # API testlari
```

Noldan tiklash kerak bo'lsa: `cp .env.example .env`, `composer install`, `php artisan key:generate`, `touch database/database.sqlite`, `php artisan migrate`, `php artisan db:seed --class=MobileDevSeeder`. `.env` da `APP_LOCALE=uz`, `LOG_STACK=single`, `BROADCAST_CONNECTION=log`, `TELEGRAM_REQUIRE_SUBSCRIPTION=false` qiling.

Sinov foydalanuvchilari (hammasining paroli `password123`, faqat lokal baza):

| Email | Rol | Nima uchun |
|---|---|---|
| `solo@example.test` | solo_learner | onboarding tugagan, to'liq kiradi |
| `newbie@example.test` | solo_learner | placement test kutadi (`onboarding_completed=false`) |
| `teacher@example.test` | teacher | ilova qo'llamaydigan rol, 403 olishi kerak |

## 7. Serverni yangilash va tarqatiladigan build

Ilova server manzilini build vaqtida oladi (`SelfLearn.Mobile.csproj`, `ApiBaseUrl`):

| Build | Server |
|---|---|
| `-c Release` | `https://self-learn.uz` |
| Debug | `http://localhost:8000` (Android emulatorda avtomatik `10.0.2.2`) |
| istalgan | `-p:ApiBaseUrl=https://...` bilan o'zgartiriladi |

Android'da oddiy `http` faqat `10.0.2.2`, `localhost`, `127.0.0.1` ga ruxsat etilgan (`Resources/xml/network_security_config.xml`), qolgan hamma narsa HTTPS.

Release APK serverga yangi API chiqarilgandan keyin qayta yig'ishsiz ishlaydi. Tartib:

1. Backend: `feature/mobile-api` branch'ini ko'rib chiqib `main` ga qo'shish va odatdagidek serverga chiqarish. Yangi migratsiya yo'q (`personal_access_tokens` jadvali bazaviy migratsiyalarda bor). Deploy'dan keyin odatdagi `php artisan queue:restart`.
2. Tekshirish: `scripts/smoke-api.sh` (parolsiz). Hammasi ✓ bo'lishi kerak. Sinov akkaunti bilan to'liq tekshiruv: `SMOKE_EMAIL=... SMOKE_PASSWORD=... scripts/smoke-api.sh`.
3. APK: `dotnet publish SelfLearn.Mobile.Android -c Release`, natija `SelfLearn.Mobile.Android/bin/Release/net10.0-android/publish/*-Signed.apk`. Hozir u debug kalit bilan imzolanadi: telefonga qo'lda o'rnatish uchun yetarli, Play Store uchun emas (6-bosqich).

2026-11-11 holati: production'da `/up` 200, `/api/v1/*` 404 (API hali chiqarilmagan). Release APK emulatordan `https://self-learn.uz` ga HTTPS orqali yetib bordi va 404 ni to'g'ri ko'rsatdi.

Eslatma: Debug build "fast deployment" ishlatadi. Emulatorda `adb shell pm clear` qilinsa ilova ochilmay qoladi; `adb uninstall` qilib, qayta `-t:Run` qiling.

## 8. Ochiq savollar
1. Productionda Telegram obuna to'sig'i yoqilganmi? Ilova ham shuni majburlashi kerakmi?
2. Ilovaning yakuniy nomi va `ApplicationId` (masalan `uz.selflearn.app`).
