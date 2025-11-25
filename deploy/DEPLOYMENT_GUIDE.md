# 🚀 Deployment Guide - DigitalOcean

Повний гайд по розгортанню Flowly на DigitalOcean.

## 📋 Передумови

- [ ] Акаунт на DigitalOcean
- [ ] Доменне ім'я (опціонально, але рекомендовано)
- [ ] SSH ключ (для безпечного доступу)

---

## Крок 1: Створення Droplet

### 1.1 Створіть новий Droplet

1. Зайдіть на [DigitalOcean](https://cloud.digitalocean.com)
2. Натисніть **Create** → **Droplets**
3. Виберіть параметри:
   - **Image:** Ubuntu 22.04 LTS
   - **Plan:** Regular - $12/міс (2GB RAM, 1 CPU, 50GB SSD)
   - **Datacenter:** Frankfurt або Amsterdam
   - **Authentication:** SSH Key (додайте свій публічний ключ)
   - **Hostname:** `flowly-prod`

4. Натисніть **Create Droplet**

### 1.2 Отримайте IP адресу

Після створення скопіюйте IP адресу вашого Droplet (наприклад: `164.92.123.45`)

---

## Крок 2: Налаштування DNS (якщо є домен)

Якщо у вас є доменне ім'я:

1. Зайдіть в панель управління вашого DNS провайдера
2. Додайте A-record:
   ```
   Type: A
   Name: @ (або flowly)
   Value: YOUR_DROPLET_IP
   TTL: 3600
   ```
3. Додайте CNAME для www:
   ```
   Type: CNAME
   Name: www
   Value: your-domain.com
   TTL: 3600
   ```

Зачекайте 5-10 хвилин для propagation DNS.

---

## Крок 3: Підключення до сервера

```bash
# Підключіться до сервера
ssh root@YOUR_DROPLET_IP

# Або якщо використовуєте SSH ключ
ssh -i ~/.ssh/your_key root@YOUR_DROPLET_IP
```

---

## Крок 4: Налаштування сервера

### 4.1 Завантажте setup скрипт

```bash
# Завантажте скрипт
curl -o setup-server.sh https://raw.githubusercontent.com/YOUR_USERNAME/Flowly/main/deploy/setup-server.sh

# Або скопіюйте вручну
nano setup-server.sh
# Вставте вміст з deploy/setup-server.sh
```

### 4.2 Запустіть setup

```bash
chmod +x setup-server.sh
./setup-server.sh
```

Скрипт встановить:
- ✅ Docker & Docker Compose
- ✅ Nginx
- ✅ Certbot (для SSL)
- ✅ Git
- ✅ Firewall (UFW)

### 4.3 Перезайдіть

```bash
exit
ssh root@YOUR_DROPLET_IP
```

---

## Крок 5: Клонування проекту

```bash
# Перейдіть в робочу директорію
cd /var/www/flowly

# Клонуйте репозиторій
git clone https://github.com/YOUR_USERNAME/Flowly.git .

# Або якщо приватний репозиторій
git clone https://YOUR_TOKEN@github.com/YOUR_USERNAME/Flowly.git .
```

---

## Крок 6: Налаштування Environment

### 6.1 Створіть .env файл

```bash
cp deploy/.env.production.server .env
nano .env
```

### 6.2 Заповніть важливі змінні

**Обов'язково змініть:**

```bash
# Генеруйте сильний пароль для БД
POSTGRES_PASSWORD=$(openssl rand -base64 32)
echo "POSTGRES_PASSWORD=$POSTGRES_PASSWORD"

# Генеруйте JWT secret
JWT_SECRET=$(openssl rand -base64 64)
echo "JWT_SECRET=$JWT_SECRET"

# Вставте ваш домен
ALLOWED_ORIGINS=https://your-domain.com
JWT_ISSUER=https://your-domain.com
JWT_AUDIENCE=https://your-domain.com

# Google OAuth credentials (з Google Cloud Console)
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
```

### 6.3 Створіть директорії для даних

```bash
mkdir -p data/postgres data/uploads
chmod 755 data
```

---

## Крок 7: Налаштування Nginx Reverse Proxy

### 7.1 Створіть конфігурацію Nginx

```bash
sudo nano /etc/nginx/sites-available/flowly
```

Вставте вміст з `deploy/nginx-config.conf` та замініть `your-domain.com` на ваш домен.

### 7.2 Активуйте конфігурацію

```bash
# Створіть symlink
sudo ln -s /etc/nginx/sites-available/flowly /etc/nginx/sites-enabled/

# Видаліть default конфігурацію
sudo rm /etc/nginx/sites-enabled/default

# Перевірте конфігурацію
sudo nginx -t

# Перезапустіть Nginx
sudo systemctl restart nginx
```

---

## Крок 8: Отримання SSL сертифікату

```bash
# Отримайте SSL сертифікат від Let's Encrypt
sudo certbot --nginx -d your-domain.com -d www.your-domain.com

# Виберіть опції:
# - Email: your-email@example.com
# - Agree to terms: Yes
# - Redirect HTTP to HTTPS: Yes (рекомендовано)
```

Certbot автоматично налаштує SSL та оновить конфігурацію Nginx.

---

## Крок 9: Запуск додатку

### 9.1 Побудуйте та запустіть контейнери

```bash
cd /var/www/flowly

# Побудуйте образи
docker-compose -f docker-compose.prod.yml build

# Запустіть контейнери
docker-compose -f docker-compose.prod.yml up -d
```

### 9.2 Перевірте статус

```bash
# Перевірте контейнери
docker-compose -f docker-compose.prod.yml ps

# Перевірте логи
docker-compose -f docker-compose.prod.yml logs -f

# Перевірте здоров'я
curl http://localhost/api/health
```

---

## Крок 10: Перевірка deployment

### 10.1 Відкрийте у браузері

```
https://your-domain.com
```

### 10.2 Перевірте функціональність

- [ ] Головна сторінка завантажується
- [ ] Google авторизація працює
- [ ] API відповідає
- [ ] SSL сертифікат валідний (замочок в браузері)

---

## 🔧 Управління

### Перезапуск

```bash
docker-compose -f docker-compose.prod.yml restart
```

### Оновлення коду

```bash
cd /var/www/flowly
git pull origin main
docker-compose -f docker-compose.prod.yml up -d --build
```

### Перегляд логів

```bash
# Всі логи
docker-compose -f docker-compose.prod.yml logs -f

# Тільки API
docker-compose -f docker-compose.prod.yml logs -f api

# Тільки Web
docker-compose -f docker-compose.prod.yml logs -f web
```

### Зупинка

```bash
docker-compose -f docker-compose.prod.yml down
```

### Бекап бази даних

```bash
# Створіть бекап
docker exec flowly-db-prod pg_dump -U flowly_user flowly_prod > backup_$(date +%Y%m%d).sql

# Відновлення з бекапу
docker exec -i flowly-db-prod psql -U flowly_user flowly_prod < backup_20231125.sql
```

---

## 🔒 Безпека

### Налаштуйте автоматичні оновлення

```bash
sudo apt install unattended-upgrades
sudo dpkg-reconfigure -plow unattended-upgrades
```

### Налаштуйте fail2ban

```bash
sudo apt install fail2ban
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

### Регулярні бекапи

Створіть cron job для автоматичних бекапів:

```bash
crontab -e

# Додайте (бекап щодня о 2:00 ночі)
0 2 * * * cd /var/www/flowly && docker exec flowly-db-prod pg_dump -U flowly_user flowly_prod > /var/backups/flowly_$(date +\%Y\%m\%d).sql
```

---

## 🆘 Troubleshooting

### Контейнер не запускається

```bash
# Перевірте логи
docker-compose -f docker-compose.prod.yml logs api

# Перевірте .env файл
cat .env
```

### Nginx помилки

```bash
# Перевірте конфігурацію
sudo nginx -t

# Перевірте логи
sudo tail -f /var/log/nginx/error.log
```

### SSL проблеми

```bash
# Перевірте сертифікат
sudo certbot certificates

# Оновіть сертифікат
sudo certbot renew
```

### База даних не підключається

```bash
# Перевірте контейнер БД
docker logs flowly-db-prod

# Перевірте підключення
docker exec flowly-db-prod psql -U flowly_user -d flowly_prod -c "SELECT 1"
```

---

## 📊 Моніторинг

### Встановіть Netdata (опціонально)

```bash
bash <(curl -Ss https://my-netdata.io/kickstart.sh)
```

Відкрийте: `http://YOUR_IP:19999`

---

## ✅ Checklist

- [ ] Droplet створено
- [ ] DNS налаштовано
- [ ] Сервер налаштовано (Docker, Nginx, Certbot)
- [ ] Проект склоновано
- [ ] .env налаштовано
- [ ] SSL сертифікат отримано
- [ ] Додаток запущено
- [ ] Все працює в браузері
- [ ] Бекапи налаштовані

---

## 🎉 Готово!

Ваш Flowly тепер працює в production на DigitalOcean!

**Корисні посилання:**
- Dashboard: https://your-domain.com
- API Health: https://your-domain.com/api/health
- DigitalOcean Panel: https://cloud.digitalocean.com
