using Microsoft.AspNetCore.Identity;

namespace Flowly.Infrastructure.Identity
{
    // Тонкий клас — залишаємо гнучкість додати поля пізніше (наприклад, DisplayName).
    public class AppUser : IdentityUser
    {
        // Тут навмисно порожньо: профіль зберігаємо у UserProfile (Domain) по UserId.
        // Чому так: відокремлюємо облікові дані (Identity) і бізнес-профіль (Domain).
    }
}