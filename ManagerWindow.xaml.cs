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

namespace Project_tipa
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class ManagerWindow : Window
    {
        private readonly Shkutan_MaximEntities1 _context;

        public ManagerWindow()
        {
            InitializeComponent();
            _context = new Shkutan_MaximEntities1();
            UpdateRoomStatuses(); // Проверяем статусы при запуске
            LoadBookings(); // Загружаем данные при открытии окна
            LoadRoomsData(); // Добавляем загрузку данных о номерах
            LoadRoomNumbers();
            LoadCleaners();
            LoadCleaningTasks(); // Загрузка задач по уборке
        }

        private void LoadBookings()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    // Загружаем связанные данные
                    var bookings = context.Reservation
                        .Include(b => b.Guests)  // Подгружаем данные о гостях
                        .Include(b => b.Rooms)  // Подгружаем данные о гостях
                        .Select(b => new
                        {
                            GuestName = b.Guests.first_name + " " + b.Guests.last_name,
                            CheckInDate = b.check_in_date,
                            CheckOutDate = b.check_out_date,
                            RoomId = b.Rooms.number,
                            Status = b.status,
                            Email = b.Guests.email,
                            DocumentNumber = b.Guests.document_number,
                        })
                        .ToList();

                    // Добавим отладочную информацию
                    if (bookings.Any())
                    {
                        MessageBox.Show($"Загружено {bookings.Count} бронирований");
                    }
                    else
                    {
                        MessageBox.Show("Бронирования не найдены");
                    }

                    BookingsGrid.ItemsSource = bookings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            LoadBookings(); // Обновление данных при нажатии кнопки
        }

        private void AddBooking_Click(object sender, RoutedEventArgs e)
        {
            var addBookingWindow = new AddBooking();
            addBookingWindow.Owner = this;

            if (addBookingWindow.ShowDialog() == true)
            {
                LoadBookings(); // Обновляем список бронирований
            }
        }

        private void LoadRoomsData()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    // Загружаем данные о номерах
                    var rooms = context.Rooms
                        .Select(r => new
                        {
                            r.number,
                            r.category,
                            r.status,
                            r.price,
                            r.floor,
                            CurrentGuest = context.Reservation
                                .Where(res => res.room_id == r.id &&
                                             res.status == "Занят" &&
                                             res.check_in_date <= DateTime.Today &&
                                             res.check_out_date >= DateTime.Today)
                                .Join(context.Guests,
                                      res => res.guest_id,
                                      g => g.id,
                                      (res, g) => g.last_name + " " + g.first_name)
                                .FirstOrDefault() ?? "Нет гостей"
                        })
                        .ToList();

                    RoomsGrid.ItemsSource = rooms;

                    // Подсчет статистики
                    int totalRooms = rooms.Count;
                    int occupiedRooms = rooms.Count(r => r.status == "Занят");
                    int cleaningRooms = rooms.Count(r => r.status == "Уборка");
                    int freeRooms = totalRooms - occupiedRooms - cleaningRooms;
                    double occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

                    // Обновляем UI
                    txtTotalRooms.Text = totalRooms.ToString();
                    txtOccupiedRooms.Text = occupiedRooms.ToString();
                    txtFreeRooms.Text = freeRooms.ToString();
                    txtCleaningRooms.Text = cleaningRooms.ToString();
                    txtOccupancyRate.Text = $"{occupancyRate:F1}%";

                    // Добавляем цветовое выделение для процента загрузки
                    if (occupancyRate >= 80)
                        txtOccupancyRate.Foreground = Brushes.Green;
                    else if (occupancyRate >= 50)
                        txtOccupancyRate.Foreground = Brushes.Orange;
                    else
                        txtOccupancyRate.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshRooms_Click(object sender, RoutedEventArgs e)
        {
            LoadRoomsData();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _context?.Dispose();
        }
        // Метод для проверки и обновления статусов комнат
        private void UpdateRoomStatuses()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    // Находим все активные бронирования с датой выезда в прошлом
                    var expiredBookings = context.Reservation
                        .Where(r => r.status == "Занят" && r.check_out_date < DateTime.Today)
                        .ToList();

                    foreach (var booking in expiredBookings)
                    {
                        // Обновляем статус бронирования
                        booking.status = "Завершено";

                        // Обновляем статус комнаты
                        var room = context.Rooms.Find(booking.room_id);
                        if (room != null)
                        {
                            room.status = "На уборке";
                        }
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статусов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoomNumbers()
        {
            try
            {
                var availableRooms = _context.Rooms
                    .Where(r => r.status == "На уборке" || r.status == "Занят") // Фильтруем по статусу
                    .Select(r => new
                    {
                        RoomId = r.id,
                        DisplayNumber = r.number,
                    })
                    .OrderBy(r => r.DisplayNumber)
                    .ToList();

                RoomNumberCombo.ItemsSource = availableRooms;
                RoomNumberCombo.SelectedValuePath = "RoomId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке номеров: {ex.Message}");
            }
        }

        private void LoadCleaners()
        {
            try
            {
                var cleaners = _context.Users
                    .Where(u => u.role == "cleaner") // Фильтруем по роли
                    .Select(u => new
                    {
                        UserId = u.id,
                        FullName = u.firstname + " " + u.lastname
                    })
                    .ToList();

                StaffCombo.ItemsSource = cleaners;
                StaffCombo.SelectedValuePath = "UserId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сотрудников: {ex.Message}");
            }
        }

        private void LoadCleaningTasks()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    var cleaningTasks = context.Cleaning_Schedule
                        .Include(ct => ct.Rooms) // Подгружаем данные о комнатах
                        .Include(ct => ct.Users)  // Подгружаем данные о сотрудниках
                        .Select(ct => new
                        {
                            RoomNumber = ct.Rooms.number,
                            CleaningDate = ct.cleaning_date,
                            StaffName = ct.Users.firstname + " " + ct.Users.lastname,
                            Status = ct.status
                        })
                        .ToList();

                    CleaningGrid.ItemsSource = cleaningTasks; // Обновляем DataGrid
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных о клининге: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AssignCleaning_Click(object sender, RoutedEventArgs e)
        {
            // Проверка, что все поля заполнены
            if (RoomNumberCombo.SelectedItem == null || CleaningDate.SelectedDate == null || StaffCombo.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Получаем данные из выпадающих списков
                int roomId = (int)RoomNumberCombo.SelectedValue;
                DateTime cleaningDate = CleaningDate.SelectedDate.Value;
                int cleanerId = (int)StaffCombo.SelectedValue;

                // Создаем новую запись в Cleaning_Schedule
                var cleaningSchedule = new Cleaning_Schedule
                {
                    room_id = roomId,
                    cleaning_date = cleaningDate,
                    cleaner_id = cleanerId,
                    status = "Запланировано" // Устанавливаем статус
                };

                using (var context = new Shkutan_MaximEntities1())
                {
                    context.Cleaning_Schedule.Add(cleaningSchedule);
                    await context.SaveChangesAsync(); // Сохраняем изменения в базе данных
                }

                // Обновляем DataGrid
                LoadCleaningTasks(); // Метод для загрузки данных в DataGrid
                MessageBox.Show("Уборка успешно назначена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show($"Ошибка при назначении уборки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}