using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Flowly.Domain.Entities;
public class UserProfile : BaseEntity
    {
        // Зв'язок із ASP.NET Core Identity (AspNetUsers.Id). string — дефолтний тип ключа.
        [Required]
        [MaxLength(256)]
        public string UserId { get; set; } = default!;

        // Тримаймо ім'я/прізвище окремо — простіше для локалізації/форм.
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Email зберігається в Identity; дубль тут не робимо, щоб уникнути розсинхронізації.
        // Якщо потрібно — можна витягати через UserManager за UserId.

        // Зберігаємо культуру як IETF language tag (наприклад, "uk", "en-US").
        // Чому string, а не enum: менше тертя з реальними culture-кодами.
        [Required, MaxLength(12)]
        public string PreferredCulture { get; set; } = "uk";

        // Відносний шлях до wwwroot/uploads/{userId}/avatar.jpg
        // Не тримаємо blob у БД — дешевше та простіше мігрувати у файли/об'єктне сховище.
        [MaxLength(512)]
        public string? AvatarPath { get; set; }

        // Зручне обчислюване поле — не мапимо в БД (EF його проігнорує).
        public string FullName => $"{FirstName} {LastName}".Trim();
    }