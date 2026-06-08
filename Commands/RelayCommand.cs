using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WarmBox_Central_Monitoring_Station.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        // 添加无参数的构造函数重载
        private readonly Action _executeNoParam;
        private readonly Func<bool> _canExecuteNoParam;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // 原有带参数的构造函数
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // 新增无参数的构造函数
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _executeNoParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteNoParam = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_executeNoParam != null)
            {
                return _canExecuteNoParam?.Invoke() ?? true;
            }
            else
            {
                return _canExecute == null || _canExecute(parameter);
            }
        }

        public void Execute(object parameter)
        {
            if (_executeNoParam != null)
            {
                _executeNoParam();
            }
            else
            {
                _execute(parameter);
            }
        }
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);

        // 修改事件定义，使用完整语法
        private event EventHandler _canExecuteChanged;
        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        // 现在可以正常调用
        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
