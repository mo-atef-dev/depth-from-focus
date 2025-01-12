using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Focus_Measurement_Tool
{
    public partial class ProgressModal : Window
    {
        private Task _task;

        public ProgressModal(Task task)
        {
            InitializeComponent();

            Mouse.OverrideCursor = Cursors.Wait;

            _task = task;
            _task.ContinueWith(t => {
                Dispatcher.Invoke(() => {
                    Mouse.OverrideCursor = Cursors.Arrow;
                    Close();
                });
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!_task.IsCompleted)
            {
                e.Cancel = true;
            }
        }
    }
}
