using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using Project_tipa;

namespace Project_tipa
{
    public partial class AddUserWindow : Window
    {
        public delegate void GridUpdatedHandler();
        public static event GridUpdatedHandler GridUpdated;
        public AddUserWindow()
        {
            InitializeComponent();
            RoleGetter.Items.Add("admin");
            RoleGetter.Items.Add("staff");
            RoleGetter.Items.Add("cleaner");
            RoleGetter.Items.Add("manager");
        }

        private void MakeUser_Click(object sender, RoutedEventArgs e)
        {   
            
            string firstname = txtFirstname.Text.Trim();
            string lastname = txtLastname.Text.Trim();
            string login = txtLogin.Text.Trim();
            string password = Kostul.ComputeSha256Hash(txtPassword.Password.Trim());
            string role = RoleGetter.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(firstname) || string.IsNullOrEmpty(lastname) ||
                string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(role))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    // Проверяем, нет ли уже пользователя с таким логином
                    if (context.Users.Any(u => u.username == login))
                    {
                        MessageBox.Show("Такой логин уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Создаем нового пользователя
                    Users newUser = new Users
                    {
                        firstname = firstname,
                        lastname = lastname,
                        username = login,
                        role = role,
                        password = password,
                        FailedLoginAttempts = 0,
                        IsLocked = false,
                        IsFirstLogin = true,
                        LastLoginDate = DateTime.Now
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    MessageBox.Show("Пользователь успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                GridUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении пользователя: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
