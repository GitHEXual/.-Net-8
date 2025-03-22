using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Project_tipa
{
    internal class Kostul
    {
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Преобразуем в шестнадцатеричную строку
                }
                return builder.ToString();
            }

        }
        
        public static void MigratePasswords()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    var users = context.Users.ToList();
                    int successCount = 0;

                    foreach (var user in users)
                    {
                        try
                        {
                            // Пропускаем пустые пароли
                            if (string.IsNullOrEmpty(user.password))
                                continue;

                            // Пропускаем уже хэшированные пароли
                            if (user.password.StartsWith("$2"))
                                continue;

                            // Хэшируем пароль
                            string hashedPassword = ComputeSha256Hash(user.password);
                            user.password = hashedPassword;
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка для пользователя {user.username}: {ex.Message}");
                        }
                    }

                    // Сохраняем изменения
                    try
                    {
                        context.SaveChanges();
                        MessageBox.Show($"Успешно обновлено паролей: {successCount}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        var errorMessages = dbEx.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => $"Пользователь {x.PropertyName}: {x.ErrorMessage}");
                        string fullErrorMessage = string.Join("\n", errorMessages);
                        MessageBox.Show($"Ошибки валидации:\n{fullErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}\n{ex.InnerException?.Message}");
            }
        }

    }

    
}




