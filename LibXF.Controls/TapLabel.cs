using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace LibXF.Controls
{
    public class TapLabel : Label
    {
        static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(TapLabel));
        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(Object), typeof(TapLabel));
        public Object CommandParameter { get => (Object)GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        public TapCommandManager Tap { get; }

        public TapLabel()
        {
            Tap = new TapCommandManager(this, CommandProperty, CommandParameterProperty);
        }
    }
}
