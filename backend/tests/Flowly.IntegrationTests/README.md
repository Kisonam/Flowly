# Flowly Backend Integration Tests

Інтеграційні тести для Flowly backend API. Тестують повні HTTP flow через весь стек додатку.

## Технології

- **xUnit** - тестовий фреймворк
- **WebApplicationFactory** - для запуску тестового web сервера
- **FluentAssertions** - для виразних assertions
- **In-Memory Database** - для ізоляції тестів

## Відмінність від Unit Tests

| Аспект | Unit Tests | Integration Tests |
|--------|-----------|-------------------|
| **Що тестують** | Окремі класи/методи ізольовано | Повні HTTP flow через API |
| **Залежності** | Мокаються | Реальні (крім БД) |
| **База даних** | SQLite In-Memory | EF Core In-Memory |
| **Швидкість** | Дуже швидкі (~2 сек) | Повільніші (~5-10 сек) |
| **Покриття** | Бізнес-логіка | End-to-end сценарії |

## Структура тестів

### AuthFlowTests (6 тестів)
Тестують повний authentication flow:

#### ✅ ТЕСТ 1: Register → Login → Access Protected Endpoint
- Реєстрація нового користувача
- Логін з отриманням JWT токенів
- Доступ до захищеного endpoint з токеном
- **Перевіряє:** Весь auth flow працює end-to-end

#### ✅ ТЕСТ 2: Refresh Token Flow
- Реєстрація та отримання токенів
- Використання refresh token для нових токенів
- Перевірка, що нові токени працюють
- **Перевіряє:** Механізм оновлення токенів

#### ✅ ТЕСТ 3: Protected Endpoint Without Token
- Спроба доступу без токену
- **Перевіряє:** Захищені endpoints недоступні без автентифікації

#### ✅ ТЕСТ 4: Protected Endpoint With Invalid Token
- Спроба доступу з невалідним токеном
- **Перевіряє:** Невалідні токени не працюють

#### ✅ ТЕСТ 5: Register With Existing Email
- Спроба створити два акаунти з одним email
- **Перевіряє:** Захист від дублювання email

#### ✅ ТЕСТ 6: Login With Wrong Password
- Спроба логіну з неправильним паролем
- **Перевіряє:** Неможливо увійти без правильного пароля

### CrudFlowTests (4 тести)
Тестують CRUD операції через API:

#### ✅ ТЕСТ 1: Note CRUD Flow
- **Create:** Створення нової нотатки
- **Get:** Отримання нотатки по ID
- **Update:** Оновлення title та content
- **Archive:** Архівування нотатки
- **Перевіряє:** Повний життєвий цикл нотатки

#### ✅ ТЕСТ 2: Task CRUD Flow
- **Create:** Створення нової задачі
- **Add Subtask:** Додавання двох підзадач
- **Toggle Subtask:** Виконання підзадачі
- **Complete:** Завершення основної задачі
- **Перевіряє:** Робота з ієрархічними структурами

#### ✅ ТЕСТ 3: Get Note From Another User
- User1 створює нотатку
- User2 намагається її отримати
- **Перевіряє:** Ізоляція даних між користувачами

#### ✅ ТЕСТ 4: Update Task From Another User
- User1 створює задачу
- User2 намагається її оновити
- **Перевіряє:** Неможливість змінити чужі дані

## FlowlyWebApplicationFactory

Custom WebApplicationFactory, який:
- Замінює реальну БД на In-Memory
- Очищає БД перед кожним тестом
- Встановлює Testing environment
- Забезпечує ізоляцію тестів

```csharp
public class FlowlyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Заміна реальної БД на In-Memory
        // Очищення БД перед кожним тестом
        // Testing environment
    }
}
```

## Запуск тестів

### Запустити всі integration тести
```bash
dotnet test tests/Flowly.IntegrationTests/Flowly.IntegrationTests.csproj
```

### З детальним виводом
```bash
dotnet test tests/Flowly.IntegrationTests/Flowly.IntegrationTests.csproj --logger "console;verbosity=detailed"
```

### Запустити конкретний клас
```bash
dotnet test --filter "FullyQualifiedName~AuthFlowTests"
dotnet test --filter "FullyQualifiedName~CrudFlowTests"
```

### Запустити конкретний тест
```bash
dotnet test --filter "FullyQualifiedName~AuthFlow_RegisterLoginAccessProtected_ShouldWorkEndToEnd"
```

## Що тестується

### ✅ HTTP Layer
- Правильні status codes (200, 201, 400, 401, 404)
- Правильні headers (Authorization, Content-Type)
- Правильна серіалізація/десеріалізація JSON

### ✅ Authentication & Authorization
- JWT токени генеруються і працюють
- Refresh tokens можна використати
- Захищені endpoints недоступні без токену
- Невалідні токени не працюють

### ✅ Data Isolation
- Користувачі не можуть отримати чужі дані
- Користувачі не можуть змінити чужі дані
- Кожен тест має ізольовану БД

### ✅ Business Logic
- CRUD операції працюють коректно
- Timestamps оновлюються
- Статуси змінюються правильно
- Ієрархічні структури (subtasks) працюють

### ✅ Validation
- Неможливо створити дублікати (email)
- Неможливо увійти з неправильним паролем
- Обов'язкові поля перевіряються

## Приклад тесту

```csharp
[Fact]
public async Task AuthFlow_RegisterLoginAccessProtected_ShouldWorkEndToEnd()
{
    // 1. Реєстрація
    var registerDto = new RegisterDto { ... };
    var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
    registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    
    // 2. Логін
    var loginDto = new LoginDto { ... };
    var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
    var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
    
    // 3. Доступ до захищеного endpoint
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);
    var profileResponse = await _client.GetAsync("/api/auth/me");
    profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Best Practices

1. **Ізоляція** - кожен тест має свою БД
2. **Реалістичність** - тестуємо через реальні HTTP запити
3. **Безпека** - особлива увага до auth та data isolation
4. **Читабельність** - чіткі коментарі та структура Arrange/Act/Assert
5. **Швидкість** - використовуємо In-Memory БД

## Коли використовувати

**Integration Tests** використовуйте для:
- ✅ Тестування повних user flows
- ✅ Перевірки роботи HTTP endpoints
- ✅ Тестування auth/authorization
- ✅ Перевірки серіалізації/десеріалізації
- ✅ E2E сценаріїв

**Unit Tests** використовуйте для:
- ✅ Тестування бізнес-логіки
- ✅ Тестування edge cases
- ✅ Тестування складних алгоритмів
- ✅ Швидкого feedback loop

## Структура проекту

```
tests/Flowly.IntegrationTests/
├── Helpers/
│   └── FlowlyWebApplicationFactory.cs  # Custom WebApplicationFactory
├── AuthFlowTests.cs                     # Auth integration tests (6 тестів)
├── CrudFlowTests.cs                     # CRUD integration tests (4 тести)
├── Flowly.IntegrationTests.csproj
└── README.md
```

## Загальна кількість: 10 integration тестів

Всі тести покривають реальні HTTP сценарії через весь стек додатку!
