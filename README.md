# CompanyApp (ASP.NET Core 9 Razor Pages + EF Core + Identity + JWT + PWA)

## Запуск
```bash
cd CompanyApp.Web
dotnet restore
dotnet ef migrations add Init
dotnet ef database update
dotnet run
```

Зайдите на адрес из консоли. Вход:
- **Email:** admin@local
- **Пароль:** Admin123$

## API
- Получить токен: `POST /api/auth/token` JSON: `{ "email": "admin@local", "password": "Admin123$" }`
- Работать с `/api/products` с заголовком `Authorization: Bearer <токен>`.

## PWA
Откройте со смартфона в Chrome/Edge и добавьте на главный экран.
