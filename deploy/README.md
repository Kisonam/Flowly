# 🚀 Flowly Deployment Files

Файли для розгортання Flowly на production сервері (DigitalOcean).

## 📁 Файли

- **`DEPLOYMENT_GUIDE.md`** - Повний покроковий гайд по deployment
- **`setup-server.sh`** - Скрипт для налаштування сервера
- **`nginx-config.conf`** - Конфігурація Nginx reverse proxy
- **`.env.production.server`** - Шаблон environment змінних для сервера

## ⚡ Швидкий старт

### 1. Створіть Droplet на DigitalOcean
- Ubuntu 22.04 LTS
- 2GB RAM мінімум
- Frankfurt/Amsterdam datacenter

### 2. Підключіться до сервера
```bash
ssh root@YOUR_DROPLET_IP
```

### 3. Завантажте та запустіть setup скрипт
```bash
curl -o setup-server.sh https://raw.githubusercontent.com/YOUR_USERNAME/Flowly/main/deploy/setup-server.sh
chmod +x setup-server.sh
./setup-server.sh
```

### 4. Клонуйте проект
```bash
cd /var/www/flowly
git clone https://github.com/YOUR_USERNAME/Flowly.git .
```

### 5. Налаштуйте .env
```bash
cp deploy/.env.production.server .env
nano .env
# Заповніть POSTGRES_PASSWORD, JWT_SECRET, GOOGLE_CLIENT_ID тощо
```

### 6. Налаштуйте Nginx
```bash
sudo cp deploy/nginx-config.conf /etc/nginx/sites-available/flowly
# Відредагуйте your-domain.com на ваш домен
sudo nano /etc/nginx/sites-available/flowly
sudo ln -s /etc/nginx/sites-available/flowly /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### 7. Отримайте SSL
```bash
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

### 8. Запустіть додаток
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### 9. Перевірте
```
https://your-domain.com
```

## 📖 Детальна інструкція

Дивіться **`DEPLOYMENT_GUIDE.md`** для повної покрокової інструкції.

## 🆘 Підтримка

Якщо виникли проблеми, перевірте:
1. Логи: `docker-compose -f docker-compose.prod.yml logs -f`
2. Nginx: `sudo tail -f /var/log/nginx/error.log`
3. Firewall: `sudo ufw status`

## 💰 Вартість

**DigitalOcean Droplet:**
- Basic: $6/міс (1GB RAM) - мінімум для тестування
- Regular: $12/міс (2GB RAM) - рекомендовано для production
- Professional: $24/міс (4GB RAM) - для високого навантаження

**Додатково:**
- Домен: ~$10-15/рік (Namecheap, Google Domains)
- SSL: Безкоштовно (Let's Encrypt)
