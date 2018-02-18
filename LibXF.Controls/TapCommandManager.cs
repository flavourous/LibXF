using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace LibXF.Controls
{
    public class TapCommandManager : BindableObject
    {
        static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(TapCommandManager), null, BindingMode.OneWay, ValidateCommand);
        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(Object), typeof(TapCommandManager), null, BindingMode.OneWay, ValidateParameter);
        public Object CommandParameter { get => (Object)GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        static readonly BindablePropertyKey CanExecuteCommandPropertyKey = BindableProperty.CreateReadOnly("CanExecuteCommand", typeof(bool), typeof(TapCommandManager), false);
        public static BindableProperty CanExecuteCommandProperty => CanExecuteCommandPropertyKey.BindableProperty;
        public bool CanExecuteCommand { get => (bool)GetValue(CanExecuteCommandProperty); }

        ICommand lastCommand = null;
        static bool ValidateCommand(BindableObject o, object v)
        {
            var @this = o as TapCommandManager;

            // Subscription managment
            if (@this.lastCommand != null) @this.lastCommand.CanExecuteChanged -= @this.LastCommand_CanExecuteChanged;
            @this.lastCommand = v as ICommand;
            if (@this.lastCommand != null) @this.lastCommand.CanExecuteChanged += @this.LastCommand_CanExecuteChanged;

            // Check can execute
            @this.LastCommand_CanExecuteChanged(@this.lastCommand, new CCEa { command = @this.lastCommand, parameter = @this.CommandParameter } );

            return true;
        }

        // TODO: use observable or something to balance Readyness of CanExecuteCommand, and cost of always reacting to CanExecuteChanged with a costly CanExecute call
        class CCEa : EventArgs { public ICommand command; public object parameter; }
        private void LastCommand_CanExecuteChanged(object sender, EventArgs e)
        {
            var ea = e as CCEa ?? new CCEa { command = Command, parameter = CommandParameter };
            SetValue(CanExecuteCommandPropertyKey, ea.command?.CanExecute(ea.parameter) ?? false);
        }

        static bool ValidateParameter(BindableObject o, object v)
        {
            var @this = o as TapCommandManager;

            // Check can execute
            @this.LastCommand_CanExecuteChanged(@this.lastCommand, new CCEa { command = @this.Command, parameter = v });

            return true;
        }
        
        public TapCommandManager(View v, BindableProperty CommandProperty, BindableProperty ParameterProperty) : this(v)
        {
            this.SetBinding(TapCommandManager.CommandProperty, new Binding { Path = CommandProperty.PropertyName, Source = v });
            this.SetBinding(TapCommandManager.CommandParameterProperty, new Binding { Path = ParameterProperty.PropertyName, Source = v });
        }
        public TapCommandManager(View v)
        {
            v.GestureRecognizers.Add(new TapGestureRecognizer(lv =>
            {
                if (CanExecuteCommand) Command.Execute(CommandParameter);
            }));
        }
    }
}
