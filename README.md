# Prava markazlari — hisob-kitob tizimi

ASP.NET Core 8 MVC + MySQL. Bir nechta o'quv markazi (filial), har bir markazning o'quvchilari va to'lovlari alohida hisoblanadi.

## Imkoniyatlar

- **Markazlar (filiallar)** — istalgancha; har birining o'quvchilari, tushumi va qarzi alohida.
- **Rollar** — *Egasi* hamma markazni ko'radi; *Markaz menejeri* faqat o'ziga biriktirilgan markazni (bazada filtr bilan cheklangan).
- **O'quvchilar** — F.I.Sh., rasm (3×4), tug'ilgan sana, telefon, pasport, manzil, toifa (A, B, BC, C...), holat (o'qimoqda / bitirgan / chiqib ketgan), shartnoma raqami, kurs narxi.
- **To'lovlar** — sana, summa, usul (naqd, karta, o'tkazma, Click/Payme), kim qabul qilgani; har to'lovdan keyingi qolgan qarz.
- **Kvitansiya** — chop etiladigan, raqamli, summa so'z bilan.
- **Boshqaruv paneli** — oylik tushum grafigi (12 oy, markazlar bo'yicha), eng katta qarzdorlar, markazlar jadvali.
- **Kassa** — sana oralig'i, markaz va to'lov usuli bo'yicha hisobot.
- **Excel eksport** — o'quvchilar va to'lovlar.
- **Amallar tarixi** — kim, qachon, nima qildi (to'lov qabul qilish/o'chirish, o'quvchi qo'shish...).
- Qidiruv, filtrlar, saralash, sahifalash, yorug'/qorong'i rejim, telefonga moslashgan dizayn.

## Lokal ishga tushirish

1. `appsettings.json` dagi `ConnectionStrings:Default` ga MySQL ma'lumotlaringizni yozing.
2. `dotnet run` — baza (`prava_markaz`) va jadvallar avtomatik yaratiladi/yangilanadi.
3. Kirish: `admin` / `admin123` — **kirgandan keyin darhol o'zgartiring**.

### Namunaviy ma'lumotlar (sinash uchun)

```bash
dotnet run -- --seed-demo
```

3 ta "Demo:" markaz, 30 o'quvchi, to'lovlar va 2 menejer qo'shadi (`menejer1`, `menejer2`, parol `menejer123`).
Serverda **ishlatmang** — keyin demo markazlarni "Markazlar" bo'limidan o'chirib yuborish mumkin.

## Serverga qo'yish

```bash
dotnet publish -c Release -o publish
```

`publish` papkasini serverga ko'chiring. Serverda `appsettings.Production.json` yarating (yoki `appsettings.json` ni tahrirlang):

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=prava_markaz;User=prava;Password=KUCHLI_PAROL;CharSet=utf8mb4;"
  },
  "Database": { "ServerVersion": "8.0.36-mysql" },
  "Admin": { "Username": "admin", "Password": "boshlang'ich-parol" }
}
```

- `ServerVersion` — serverdagi MySQL versiyasi (`SELECT VERSION();`). MariaDB bo'lsa masalan `10.11.6-mariadb`.
- `Admin` faqat birinchi ishga tushishda (bazada foydalanuvchi bo'lmasa) ishlatiladi.
- Ilova ishga tushganda migratsiyalar avtomatik qo'llanadi.
- Rasmlar va cookie kalitlari `App_Data/` papkasida saqlanadi — uni zaxiralang. Boshqa joyga qo'ymoqchi bo'lsangiz `Storage:DataPath` ni ko'rsating. Ilova shu papkaga yoza olishi kerak.
- Nginx/IIS orqasida ishlaydi (X-Forwarded-* qo'llab-quvvatlanadi). Agar Kestrel o'zi HTTPS ni boshqarsa, `"UseHttpsRedirection": true` qiling.
- Rasm yuklash 5 MB gacha; Nginx ishlatsangiz `client_max_body_size 10m;` qo'shing.

### Linux (systemd) misol

```ini
[Unit]
Description=Prava markazlari
After=network.target mysql.service

[Service]
WorkingDirectory=/var/www/pravamarkaz
ExecStart=/usr/bin/dotnet /var/www/pravamarkaz/PravaMarkaz.dll --urls http://127.0.0.1:5000
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production
User=www-data

[Install]
WantedBy=multi-user.target
```

### Zaxira nusxa

```bash
mysqldump -u prava -p prava_markaz > prava_markaz_$(date +%F).sql
```
va `App_Data/uploads` papkasi.

## Bazani o'zgartirish (keyinchalik yangi maydon qo'shsangiz)

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add YangiOzgarish -o Data/Migrations
```
