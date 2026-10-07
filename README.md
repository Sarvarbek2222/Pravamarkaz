# Prava markazlari â€” hisob-kitob tizimi

ASP.NET Core 8 MVC + MySQL. Bir nechta o'quv markazi (filial), har bir markazning o'quvchilari va to'lovlari alohida hisoblanadi.

## Imkoniyatlar

- **Markazlar (filiallar)** â€” istalgancha; har birining o'quvchilari, tushumi va qarzi alohida.
- **Rollar** â€” *Egasi* hamma markazni ko'radi; *Markaz menejeri* faqat o'ziga biriktirilgan markazni (bazada filtr bilan cheklangan).
- **O'quvchilar** â€” F.I.Sh., rasm (3Ã—4), tug'ilgan sana, telefon, pasport, manzil, toifa (A, B, BC, C...), holat (o'qimoqda / bitirgan / chiqib ketgan), shartnoma raqami, kurs narxi.
- **To'lovlar** â€” sana, summa, usul (naqd, karta, o'tkazma, Click/Payme), kim qabul qilgani; har to'lovdan keyingi qolgan qarz.
- **Kvitansiya** â€” chop etiladigan, raqamli, summa so'z bilan.
- **Boshqaruv paneli** â€” oylik tushum grafigi (12 oy, markazlar bo'yicha), eng katta qarzdorlar, markazlar jadvali.
- **Kassa** â€” sana oralig'i, markaz va to'lov usuli bo'yicha hisobot.
- **Excel eksport** â€” o'quvchilar va to'lovlar.
- **Amallar tarixi** â€” kim, qachon, nima qildi (to'lov qabul qilish/o'chirish, o'quvchi qo'shish...).
- Qidiruv, filtrlar, saralash, sahifalash, yorug'/qorong'i rejim, telefonga moslashgan dizayn.

## Lokal ishga tushirish

1. `appsettings.json` dagi `ConnectionStrings:Default` ga MySQL ma'lumotlaringizni yozing.
2. `dotnet run` â€” baza (`prava_markaz`) va jadvallar avtomatik yaratiladi/yangilanadi.
3. Kirish: `admin` / `admin123` â€” **kirgandan keyin darhol o'zgartiring**.

### Namunaviy ma'lumotlar (sinash uchun)

```bash
dotnet run -- --seed-demo
```

3 ta "Demo:" markaz, 30 o'quvchi, to'lovlar va 2 menejer qo'shadi (`menejer1`, `menejer2`, parol `menejer123`).
Serverda **ishlatmang** â€” keyin demo markazlarni "Markazlar" bo'limidan o'chirib yuborish mumkin.

## Serverga qo'yish (GitHub orqali, Ubuntu/Debian)

Repo: https://github.com/Sarvarbek2222/Pravamarkaz

> âš ï¸ Repo **public** â€” haqiqiy parollarni hech qachon `appsettings.json` ga yozib GitHub'ga yuklamang.
> Serverdagi parollar faqat serverdagi `appsettings.Production.json` da turadi (u git'ga kirmaydi).

### 1. Bir martalik tayyorlov

```bash
# .NET 8 SDK, git, rsync, nginx
sudo apt update
sudo apt install -y dotnet-sdk-8.0 git rsync nginx

# MySQL baza va foydalanuvchi
sudo mysql -e "CREATE DATABASE prava_markaz CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'prava'@'localhost' IDENTIFIED BY 'KUCHLI_PAROL';
GRANT ALL PRIVILEGES ON prava_markaz.* TO 'prava'@'localhost'; FLUSH PRIVILEGES;"

# Kodni yuklab olish
sudo mkdir -p /opt/pravamarkaz /var/www/pravamarkaz
sudo chown -R $USER /opt/pravamarkaz /var/www/pravamarkaz
git clone https://github.com/Sarvarbek2222/Pravamarkaz.git /opt/pravamarkaz/src
```

Serverdagi sozlamalar â€” `/var/www/pravamarkaz/appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=prava_markaz;User=prava;Password=KUCHLI_PAROL;CharSet=utf8mb4;"
  },
  "Database": { "ServerVersion": "8.0.36-mysql" },
  "Admin": { "Username": "admin", "Password": "BOSHLANGICH_PAROL" }
}
```

- `ServerVersion` â€” `mysql -V` yoki `SELECT VERSION();` natijasi. MariaDB bo'lsa masalan `10.11.6-mariadb`.
- `Admin` faqat birinchi ishga tushishda (bazada foydalanuvchi bo'lmasa) ishlatiladi.

Xizmat va Nginx:

```bash
sudo cp /opt/pravamarkaz/src/deploy/pravamarkaz.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable pravamarkaz

sudo cp /opt/pravamarkaz/src/deploy/nginx.conf /etc/nginx/sites-available/pravamarkaz
sudo nano /etc/nginx/sites-available/pravamarkaz        # server_name ga domeningizni yozing
sudo ln -s /etc/nginx/sites-available/pravamarkaz /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# HTTPS (domen bo'lsa)
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d prava.example.uz
```

### 2. Birinchi ishga tushirish va har safar yangilash

```bash
# Rasmlar va kalitlar papkasi â€” ilova (www-data) yoza olishi kerak
sudo mkdir -p /var/www/pravamarkaz/App_Data
sudo chown -R www-data:www-data /var/www/pravamarkaz/App_Data

bash /opt/pravamarkaz/src/deploy/deploy.sh
```

Keyinchalik kompyuterda o'zgartirib GitHub'ga `git push` qilsangiz, serverda faqat:

```bash
bash /opt/pravamarkaz/src/deploy/deploy.sh
```

Skript: `git pull` â†’ `dotnet publish` â†’ xizmatni to'xtatish â†’ fayllarni ko'chirish (`App_Data` va `appsettings.Production.json` ga tegmaydi) â†’ qayta ishga tushirish. Baza migratsiyalari ilova ishga tushganda avtomatik qo'llanadi.

Loglar: `sudo journalctl -u pravamarkaz -f`

### Eslatmalar

- Rasmlar va cookie kalitlari `App_Data/` papkasida â€” uni zaxiralang. Boshqa joy uchun `Storage:DataPath`.
- Nginx orqasida ishlaydi (X-Forwarded-* qo'llab-quvvatlanadi). Kestrel o'zi HTTPS ni boshqarsa, `"UseHttpsRedirection": true`.

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
