using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace Project_tipa
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AddBooking : Window
    {
        private readonly Shkutan_MaximEntities1 _context;

        public AddBooking()
        {
            InitializeComponent();
            _context = new Shkutan_MaximEntities1();
            LoadRooms();
            
            // Установка минимальной даты для выбора
            dpCheckIn.DisplayDateStart = DateTime.Today;
            dpCheckOut.DisplayDateStart = DateTime.Today.AddDays(1);
        }

        private void LoadRooms()
        {
            try
            {
                var availableRooms = _context.Rooms
                    .Where(r => r.status != "Занят" && r.status != "Уборка")
                    .Select(r => new
                    {
                        RoomId = r.id,
                        DisplayNumber = r.number,
                        Category = r.category,
                        Price = r.price
                    })
                    .OrderBy(r => r.DisplayNumber)
                    .ToList();

                cmbRoomNumber.ItemsSource = availableRooms;
                cmbRoomNumber.SelectedValuePath = "RoomId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке номеров: {ex.Message}");
            }
        }

        private bool IsRoomAvailable(int roomId, DateTime checkInDate, out DateTime? lastCheckOutDate)
        {
            lastCheckOutDate = null;
            
            // Ищем последнее активное бронирование для этого номера
            var existingBooking = _context.Reservation
                .Where(r => r.room_id == roomId && 
                            r.status == "Занят" &&
                            r.check_out_date >= DateTime.Today)
                .OrderByDescending(r => r.check_out_date)
                .FirstOrDefault();

            if (existingBooking != null)
            {
                lastCheckOutDate = existingBooking.check_out_date;
                // Проверяем, что дата заезда хотя бы на день позже даты выезда предыдущего гостя
                return checkInDate > existingBooking.check_out_date;
            }

            return true; // Номер свободен
        }

        private void AddBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка заполнения всех полей
                if (string.IsNullOrEmpty(txtFirstName.Text) ||
                    string.IsNullOrEmpty(txtLastName.Text) ||
                    string.IsNullOrEmpty(txtEmail.Text) ||
                    string.IsNullOrEmpty(txtDocumentNumber.Text) ||
                    cmbRoomNumber.SelectedItem == null ||
                    dpCheckIn.SelectedDate == null ||
                    dpCheckOut.SelectedDate == null)
                {
                    MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка дат
                if (dpCheckIn.SelectedDate >= dpCheckOut.SelectedDate)
                {
                    MessageBox.Show("Дата выезда должна быть позже даты заезда", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранный номер комнаты
                int roomId = (int)cmbRoomNumber.SelectedValue;

                // Проверка доступности номера
                DateTime? lastCheckOutDate;
                if (!IsRoomAvailable(roomId, dpCheckIn.SelectedDate.Value, out lastCheckOutDate))
                {
                    MessageBox.Show($"Номер занят до: {lastCheckOutDate?.ToString("dd.MM.yyyy")}\n" +
                                  $"Пожалуйста, выберите дату заезда после {lastCheckOutDate?.ToString("dd.MM.yyyy")}", 
                                  "Номер недоступен", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                // Создание нового гостя
                var guest = new Guests
                {

                    first_name = txtFirstName.Text,
                    last_name = txtLastName.Text,
                    email = txtEmail.Text,
                    document_number = txtDocumentNumber.Text,
                    check_in = dpCheckIn.SelectedDate.Value,
                    check_out = dpCheckOut.SelectedDate.Value,
                };

                _context.Guests.Add(guest);
                _context.SaveChanges();

                //Создание бронирования
                MessageBox.Show($"{dpCheckOut.SelectedDate.Value}, {dpCheckIn.SelectedDate.Value}");
                var booking = new Reservation
                {
                    guest_id = guest.id,
                    room_id = roomId,
                    check_in_date = dpCheckIn.SelectedDate.Value,
                    check_out_date = dpCheckOut.SelectedDate.Value,
                    status = "Занят"
                };

                _context.Reservation.Add(booking);
                _context.SaveChanges();

                // Обновляем статус комнаты
                var room = _context.Rooms.Find(roomId);
                if (room != null)
                {
                    room.status = "Занят";
                }

                _context.SaveChanges();

                MessageBox.Show("Бронирование успешно добавлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении бронирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dpCheckIn_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRoomNumber.SelectedItem != null && dpCheckIn.SelectedDate.HasValue)
            {
                var selectedRoom = cmbRoomNumber.SelectedItem as dynamic;
                int selectedRoomId = selectedRoom.RoomId;
                DateTime checkInDate = dpCheckIn.SelectedDate.Value;
                
                DateTime? lastCheckOutDate;
                if (!IsRoomAvailable(selectedRoomId, checkInDate, out lastCheckOutDate))
                {
                    MessageBox.Show($"Номер занят до: {lastCheckOutDate?.ToString("dd.MM.yyyy")}\n" +
                                  $"Пожалуйста, выберите дату заезда после {lastCheckOutDate?.ToString("dd.MM.yyyy")}", 
                                  "Предупреждение", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _context?.Dispose();
        }
    }
}
