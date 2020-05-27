using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wpf.Utilities.Controls
{
    /// <summary>
    /// 时间选择器
    /// 支持日期、时间点选择
    /// </summary>
    [TemplatePart(Name = PART_DatePicker, Type = typeof(DatePicker))]
    [TemplatePart(Name = PART_TextBox_Hour, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_TextBox_Minute, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_TextBox_Second, Type = typeof(TextBox))]
    public class TimePicker : Control
    {
        const string PART_DatePicker = nameof(PART_DatePicker);
        const string PART_TextBox_Hour = nameof(PART_TextBox_Hour);
        const string PART_TextBox_Minute = nameof(PART_TextBox_Minute);
        const string PART_TextBox_Second = nameof(PART_TextBox_Second);
        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(nameof(SelectedTime), typeof(DateTime), typeof(TimePicker), new PropertyMetadata());
        public static readonly DependencyProperty EditModeProperty =
            DependencyProperty.Register(nameof(EditMode), typeof(TimeEditMode), typeof(TimePicker), new PropertyMetadata(TimeEditMode.Both));
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        private DatePicker _datePicker;
        private TextBox _hourTextBox;
        private TextBox _minuteTextBox;
        private TextBox _secondTextBox;
        public DateTime SelectedTime
        {
            get { return (DateTime)GetValue(SelectedTimeProperty); }
            set { SetValue(SelectedTimeProperty, value); }
        }
        public TimeEditMode EditMode
        {
            get { return (TimeEditMode)GetValue(EditModeProperty); }
            set { SetValue(EditModeProperty, value); }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _datePicker = this.Template.FindName(PART_DatePicker, this) as DatePicker;
            _hourTextBox = this.Template.FindName(PART_TextBox_Hour, this) as TextBox;
            _minuteTextBox = this.Template.FindName(PART_TextBox_Minute, this) as TextBox;
            _secondTextBox = this.Template.FindName(PART_TextBox_Second, this) as TextBox;
            if (_datePicker != null)
            {
                _datePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            }
            if (_hourTextBox != null)
            {
                InputMethod.SetIsInputMethodEnabled(_hourTextBox, false);
                _hourTextBox.TextChanged += HourTextBox_TextChanged;
                _hourTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _hourTextBox.GotFocus += TextBox_GotFocus;
            }
            if (_minuteTextBox != null)
            {
                InputMethod.SetIsInputMethodEnabled(_minuteTextBox, false);
                _minuteTextBox.TextChanged += MinuteTextBox_TextChanged;
                _minuteTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _minuteTextBox.GotFocus += TextBox_GotFocus;
            }
            if (_secondTextBox != null)
            {
                InputMethod.SetIsInputMethodEnabled(_secondTextBox, false);
                _secondTextBox.TextChanged += SecondTextBox_TextChanged;
                _secondTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _secondTextBox.GotFocus += TextBox_GotFocus;
            }

            var time = SelectedTime;
            RenderDate(time);
            RenderText(_hourTextBox, time.Hour);
            RenderText(_minuteTextBox, time.Minute);
            RenderText(_secondTextBox, time.Second);
        }
        private void RenderDate(DateTime time)
        {
            if (_datePicker != null)
            {
                _datePicker.SelectedDate = time.Date;
            }
        }
        private void RenderText(TextBox textBox, int number)
        {
            if (textBox != null)
            {
                textBox.Text = number.ToString("00");
                if (textBox.IsFocused)
                    textBox.SelectAll();
            }
        }
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var datePicker = (DatePicker)sender;
            var selectedDate = datePicker.SelectedDate;
            var time = SelectedTime;
            if (selectedDate == null)
                RenderDate(time);
            else
                SelectedTime = selectedDate.Value + time.TimeOfDay;
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            this.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            var key = e.Key;
            if ((key == Key.Back || (key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9)) == false)
            {
                e.Handled = true;
            }
        }
        private void HourTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;
            int hour = 0;
            if (string.IsNullOrEmpty(text) || text.Length > 2 || int.TryParse(text, out hour) == false)
            {
                RenderText(textBox, 0);
                return;
            }
            else if (hour > 23)
            {
                RenderText(textBox, 23);
                return;
            }
            else if (text.Length == 2)
            {
                var changedItem = e.Changes.FirstOrDefault();
                if (changedItem != null && changedItem.AddedLength == 1 && changedItem.RemovedLength == 0)
                {
                    _minuteTextBox?.Focus();
                }
            }
            var time = SelectedTime;
            SelectedTime = new DateTime(time.Year, time.Month, time.Day, hour, time.Minute, time.Second);
        }
        private void MinuteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;
            int minute = 0;
            if (string.IsNullOrEmpty(text) || text.Length > 2 || int.TryParse(textBox.Text, out minute) == false)
            {
                RenderText(textBox, 0);
                return;
            }
            else if (minute > 59)
            {
                RenderText(textBox, 59);
                return;
            }
            else if (text.Length == 2)
            {
                var changedItem = e.Changes.FirstOrDefault();
                if (changedItem != null && changedItem.AddedLength == 1 && changedItem.RemovedLength == 0)
                {
                    _secondTextBox?.Focus();
                }
            }
            var time = SelectedTime;
            SelectedTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, minute, time.Second);
        }
        private void SecondTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;
            int second = 0;
            if (string.IsNullOrEmpty(text) || text.Length > 2 || int.TryParse(textBox.Text, out second) == false)
            {
                RenderText(textBox, 0);
                return;
            }
            else if (second > 59)
            {
                RenderText(textBox, 59);
                return;
            }
            var time = SelectedTime;
            SelectedTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, second);
        }
    }
    public enum TimeEditMode
    {
        Both,
        Date,
        TimeOfDay,
    }
}
