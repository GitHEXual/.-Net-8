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
using System.Windows.Shapes;
using System.Data.Entity;
using Project_tipa;

namespace Project_tipa
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CleanerWindow : Window
    {
        private int _cleanerId; // Идентификатор уборщика

        public CleanerWindow(int cleanerId)
        {
            InitializeComponent();
            _cleanerId = cleanerId;
            LoadAssignedRooms();
        }

        public class CleaningTaskInfo
        {
            public int RoomNumber { get; set; }
            public DateTime CleaningDate { get; set; }
            public string Status { get; set; }
        }

        private void LoadAssignedRooms()
        {
            try
            {
                using (var context = new Shkutan_MaximEntities1())
                {
                    var assignedRooms = context.Cleaning_Schedule
                        .Include(cs => cs.Rooms)
                        .Where(cs => cs.cleaner_id == _cleanerId && cs.status == "Запланировано")
                        .Select(cs => new CleaningTaskInfo
                        {
                            RoomNumber = cs.Rooms.number,
                            CleaningDate = cs.cleaning_date,
                            Status = cs.status
                        })
                        .ToList();

                    AssignedRoomsGrid.ItemsSource = assignedRooms;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке назначенных комнат: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var assignedRoom = button?.DataContext as CleaningTaskInfo;

            if (assignedRoom != null)
            {
                try
                {
                    using (var context = new Shkutan_MaximEntities1())
                    {
                        var cleaningTask = context.Cleaning_Schedule
                            .Include(cs => cs.Rooms)
                            .Where(cs => cs.Rooms.number == assignedRoom.RoomNumber &&
                                   cs.cleaning_date == assignedRoom.CleaningDate &&
                                   cs.cleaner_id == _cleanerId)
                            .FirstOrDefault();

                        if (cleaningTask != null)
                        {
                            cleaningTask.status = "Завершено";

                            var room = context.Rooms.Find(cleaningTask.room_id);
                            if (room != null)
                            {
                                room.status = "Свободен";
                            }

                            context.SaveChanges();
                            LoadAssignedRooms();
                            MessageBox.Show("Статус уборки обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при изменении статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
