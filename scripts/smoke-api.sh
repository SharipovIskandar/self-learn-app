#!/usr/bin/env bash
# Backend API mobil ilova uchun tayyormi? Serverni yangilagandan (deploy) keyin ishga tushiring.
#
#   scripts/smoke-api.sh                         # https://self-learn.uz ni tekshiradi
#   scripts/smoke-api.sh http://127.0.0.1:8000   # lokal backend
#
# Standart holatda hech qanday parol yuborilmaydi: faqat "API chiqarilganmi va JSON qaytaradimi" tekshiriladi.
# Haqiqiy login/me/logout ni ham tekshirish uchun sinov akkauntini muhit o'zgaruvchisi orqali bering
# (parol buyruq satrida va shell tarixida qolmasligi uchun argument emas):
#
#   SMOKE_EMAIL=... SMOKE_PASSWORD=... scripts/smoke-api.sh

set -u

BASE="${1:-https://self-learn.uz}"
BASE="${BASE%/}"
fail=0
body_file="$(mktemp)"
trap 'rm -f "$body_file"' EXIT

ok() { printf '  \342\234\223 %s\n' "$1"; }
bad() { printf '  \342\234\227 %s\n' "$1"; fail=1; }

# check NOM KUTILGAN HAQIQIY
check() {
  if [ "$2" = "$3" ]; then ok "$1 ($3)"; else bad "$1: kutilgan $2, keldi $3"; fi
}

# req METHOD PATH [curl argumentlari...]  -> HTTP status chiqaradi, javob tanasi $body_file da
req() {
  local method="$1" path="$2"
  shift 2
  # --retry: vaqtinchalik tarmoq uzilishi (status 000) yolg'on xato bo'lib ko'rinmasin.
  curl -s -m 20 --retry 2 --retry-all-errors --retry-delay 1 \
    -o "$body_file" -w '%{http_code}' -X "$method" -H 'Accept: application/json' "$@" "$BASE$path"
}

# json a.b.c  -> oxirgi javobdan maydon (yo'q bo'lsa bo'sh satr)
json() {
  python3 -I -c '
import json, sys
try:
    value = json.load(open(sys.argv[1]))
    for key in sys.argv[2].split("."):
        value = value[key]
    print(value)
except Exception:
    print("")
' "$body_file" "$1"
}

echo "Server: $BASE"

echo "1. Server ishlayaptimi"
check "GET /up" 200 "$(req GET /up)"

echo "2. API v1 chiqarilganmi (parolsiz)"
check "GET /api/v1/me tokensiz" 401 "$(req GET /api/v1/me)"
status="$(req POST /api/v1/auth/login -H 'Content-Type: application/json' -d '{}')"
check "POST /api/v1/auth/login bo'sh tana bilan" 422 "$status"
if [ "$status" = "422" ]; then
  if [ -n "$(json errors.device_name)" ]; then ok "xato javobi JSON (errors.device_name bor)"; else bad "422 keldi, lekin errors.device_name yo'q"; fi
fi

if [ -n "${SMOKE_EMAIL:-}" ] && [ -n "${SMOKE_PASSWORD:-}" ]; then
  echo "3. Haqiqiy kirish ($SMOKE_EMAIL)"
  login_body="$(python3 -I -c 'import json, os; print(json.dumps({"email": os.environ["SMOKE_EMAIL"], "password": os.environ["SMOKE_PASSWORD"], "device_name": "smoke-test"}))')"
  status="$(printf '%s' "$login_body" | req POST /api/v1/auth/login -H 'Content-Type: application/json' --data-binary @-)"
  check "login" 200 "$status"
  token="$(json token)"

  if [ -n "$token" ]; then
    check "GET /api/v1/me token bilan" 200 "$(req GET /api/v1/me -H "Authorization: Bearer $token")"
    echo "    foydalanuvchi: $(json user.email), rol: $(json user.role), onboarding: $(json user.onboarding_completed)"
    check "POST /api/v1/auth/logout" 204 "$(req POST /api/v1/auth/logout -H "Authorization: Bearer $token")"
    check "logout'dan keyin token yaroqsiz" 401 "$(req GET /api/v1/me -H "Authorization: Bearer $token")"
  else
    bad "token kelmadi: $(json message)"
  fi
else
  echo "3. Haqiqiy kirish o'tkazib yuborildi (SMOKE_EMAIL va SMOKE_PASSWORD berilmagan)"
fi

echo
if [ "$fail" = "0" ]; then
  echo "Hammasi joyida: ilova bu server bilan ishlaydi."
else
  echo "Muammo bor: yuqoridagi X belgili qatorlarga qarang. (404 bo'lsa, API bu serverga hali chiqarilmagan.)"
fi

exit "$fail"
