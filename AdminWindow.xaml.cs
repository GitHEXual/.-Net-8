using System;
using System.Collections.Generic;
using System.Data.Entity;
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

namespace Project_tipa
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            LoadUsers(); // Добавляем загрузку при открытии окна
        }

        private void LoadUsers()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    Users.ItemsSource = context.Users.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Users_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadUsers(); // Можно убрать отсюда, так как это создает лишнюю нагрузку
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var addUserWindow = new AddUserWindow();
            addUserWindow.Owner = this;
            
            if (addUserWindow.ShowDialog() == true)
            {
                LoadUsers(); // Используем тот же метод для обновления
            }
        }

        private async void UnlockUser_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранного пользователя
            var selectedUser = Users.SelectedItem as Users;
            
            if (selectedUser != null)
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    var user = await context.Users.FindAsync(selectedUser.id);
                    if (user != null)
                    {
                        user.IsLocked = false;
                        user.LastLoginDate = DateTime.Now;
                        user.FailedLoginAttempts = 0; // Сбрасываем счетчик неудачных попыток
                        await context.SaveChangesAsync();
                        
                        LoadUsers(); // Обновляем данные в таблице
                        MessageBox.Show("Пользователь разблокирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для разблокировки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new Shkutan_MaximEntities1())
            {
                foreach (var user in Users.ItemsSource as IEnumerable<Users>)
                {
                    var existstringUser = await context.Users.FindAsync(user.id);
                    if (existstringUser != null)
                    {
                        existstringUser.lastname = user.lastname;
                        existstringUser.firstname = user.firstname;
                        existstringUser.role = user.role;
                        existstringUser.username = user.username;
                        existstringUser.IsLocked = user.IsLocked;
                    }
                }
                await context.SaveChangesAsync();
                LoadUsers();

            }
            //LoadUsers();
        }
    }
}
