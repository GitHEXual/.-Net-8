using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.Entity;
using System.Diagnostics.Eventing.Reader;
using Project_tipa;
using System.Security.Cryptography;

namespace Project_tipa
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.");
                return;
            }


            using (var context = new Shkutan_MaximEntities1())
            {
                var user = await context.Users
                    .Where(u => u.username == username)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    MessageBox.Show("Неправильный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.IsLocked.HasValue && user.IsLocked.Value)
                {
                    MessageBox.Show("Вы заблокированы. Обратитесь к администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (user.LastLoginDate.HasValue && (DateTime.Now - user.LastLoginDate.Value).TotalDays > 30 && user.role != "admin")
                {
                    user.IsLocked = true;
                    await context.SaveChangesAsync();
                    MessageBox.Show("Ваша учетная запись заблокирована из-за длительного отсутствия входа. Обратитесь к администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (user.password == Kostul.ComputeSha256Hash(password))
                {
                    user.LastLoginDate = DateTime.Now;
                    user.FailedLoginAttempts = 0;
                    await context.SaveChangesAsync();

                    MessageBox.Show("Вы успешно авторизовались", "Добро пожаловать", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (user.IsFirstLogin.HasValue && user.IsFirstLogin.Value)
                    {
                        ChangePassword changePasswordWindow = new ChangePassword(user.id);
                        changePasswordWindow.Owner = this;
                        changePasswordWindow.ShowDialog();
                    }
                    else
                    {
                        if (user.role == "admin")
                        {
                            AdminWindow adminWindow = new AdminWindow();
                            adminWindow.Show();
                        }
                        if (user.role == "manager")
                        {
                            ManagerWindow managerWindow = new ManagerWindow();
                            managerWindow.Show();
                        }
                        if (user.role == "cleaner")
                        {
                            CleanerWindow cleanerWindow = new CleanerWindow(user.id);
                            cleanerWindow.Show();
                        }
                        else
                        {

                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                        }
                        this.Close();

                    }
                }
                else
                {
                    user.FailedLoginAttempts++;

                    if (user.FailedLoginAttempts >= 3)
                    {
                        user.IsLocked = true;
                        MessageBox.Show("Вы заблокированы после 3 неудачных попыток. Обратитесь к администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        int attemptsLeft = 3 - (user.FailedLoginAttempts ?? 0);
                        MessageBox.Show($"Неправильный логин или пароль. Осталось попыток: {attemptsLeft}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}